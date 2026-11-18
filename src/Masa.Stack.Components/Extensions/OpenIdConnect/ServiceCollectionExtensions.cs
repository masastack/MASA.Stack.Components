using Masa.Stack.Components.Infrastructure.Impersonation;

namespace Masa.Stack.Components.Extensions.OpenIdConnect;

public static class ServiceCollectionExtensions
{
    public static async Task<IServiceCollection> AddMasaOpenIdConnectAsync(
        this WebAssemblyHostBuilder builder,
        IConfiguration configuration)
    {
        var options = configuration.GetSection("$public.OIDC").Get<MasaOpenIdConnectOptions>();
        return await builder.AddMasaOpenIdConnectAsync(options);
    }

    public static async Task<IServiceCollection> AddMasaOpenIdConnectAsync(
        this WebAssemblyHostBuilder builder,
        MasaOpenIdConnectOptions masaOpenIdConnectOptions)
    {
        builder.Services.AddSingleton(masaOpenIdConnectOptions);
        builder.Services.AddSingleton<LogoutSessionManager>();
        builder.Services.TryAddScoped<CookieStorage>();
        return await builder.AddMasaOpenIdConnectAsync(
            masaOpenIdConnectOptions.Authority,
            masaOpenIdConnectOptions.ClientId,
            masaOpenIdConnectOptions.ClientSecret,
            masaOpenIdConnectOptions.Scopes.ToArray());
    }

    private static async Task<IServiceCollection> AddMasaOpenIdConnectAsync(
        this WebAssemblyHostBuilder builder,
        string authority,
        string clientId,
        string clientSecret,
        params string[] scopes)
    {
        // 模拟登录接收端：注册会话（唯一事实源，状态持久化于 localStorage，跨 scope 可见）。
        // TryAdd 而非 Add：AddMasaStackComponent 也会兜底注册同一个服务，两边都幂等才不会重复注册。
        builder.Services.TryAddScoped<ImpersonationSession>();

        var serviceProvider = builder.Services.BuildServiceProvider();

        // 模拟登录接收端：在 OIDC 初始化之前消费 URL fragment（#access_token=...&expires_in=...）。
        // 必须先于 AddOidcAuthentication——把 token 取走并清 URL 后，登录重定向才不会丢失模拟态，
        // 标准 oidc-client 也不会看到与己无关的 fragment 参数。
        // 注意：这里用的是临时 provider 的一次性实例，捕获结果只经 localStorage 传递，
        // 用完即释放（否则它持有的 ES module 代理会一直挂着）。
        try
        {
            var impersonationSession = serviceProvider.GetRequiredService<ImpersonationSession>();
            try
            {
                await impersonationSession.CaptureFromUrlAsync();
            }
            finally
            {
                await impersonationSession.DisposeAsync();
            }
        }
        catch (Exception)
        {
            // JS interop 不可用等极端场景：放弃捕获，不影响正常登录
        }

        var storage = serviceProvider.GetRequiredService<CookieStorage>();
        var masaStackConfig = serviceProvider.GetRequiredService<IMasaStackConfig>();

        var environment = await storage.GetAsync(Consts.ENVIRONMENT);

        if (string.IsNullOrEmpty(environment))
        {
            environment = masaStackConfig.Environment;
        }

        builder.Services.AddOidcAuthentication(options =>
        {
            options.ProviderOptions.Authority = authority;
            options.ProviderOptions.ClientId = clientId;
            options.ProviderOptions.ResponseType = "code";
            options.ProviderOptions.DefaultScopes.Clear();
            foreach (var scope in scopes)
            {
                options.ProviderOptions.DefaultScopes.Add(scope);
            }
            options.ProviderOptions.AdditionalProviderParameters[Consts.ENVIRONMENT] = environment;
        });

        // 模拟登录接收端：统一装饰器（单实例兼任 AuthenticationStateProvider 与 IAccessTokenProvider）
        //  - UI 身份：AuthorizeView/MasaUser/用户名全按被模拟用户渲染
        //  - API 身份：Caller Bearer 用被模拟用户的短命 token
        // 两层同源于 ImpersonationSession（同一模拟 token），杜绝"页面显示本人、操作是被模拟者"的半混合态。
        // 注意：不单独 Decorate<IAccessTokenProvider>——标准库的 IAccessTokenProvider 注册是
        // 「转发到 AuthenticationStateProvider 并强转」的工厂，AuthenticationStateProvider 被替换后
        // 该转发会自动取到本装饰实例；而本实例实现了 IAccessTokenProvider，强转成立。
        builder.Services.Decorate<AuthenticationStateProvider>((sp, inner) =>
            new ImpersonatedAuthenticationStateProvider(
                inner,
                sp.GetRequiredService<ImpersonationSession>(),
                sp.GetService<ILogger<ImpersonatedAuthenticationStateProvider>>()));

        builder.Services.AddAuthorizationCore(options =>
        {
            options.FallbackPolicy = options.DefaultPolicy;
        });

        return builder.Services;
    }

    /// <summary>
    /// 用装饰器替换既有服务注册。兼容 type-based / factory-based / instance 三种注册。
    /// </summary>
    /// <remarks>
    /// 不能用 <c>ActivatorUtilities.CreateInstance</c> 构建 inner：它按「参数最多且可满足」的启发式挑构造函数，
    /// 而 <c>RemoteAuthenticationService</c> 同时有 4 参和 5 参两个 public 构造函数，一旦某个依赖
    /// （如 <c>ILogger&lt;&gt;</c>）在宿主里不可解析，就会抛
    /// 「Multiple constructors accepting all given argument types have been found」——
    /// 这会在应用启动阶段炸掉，且错误信息完全指不到这里。下面改为显式挑构造函数，行为确定、可诊断。
    /// 生命周期沿用原始注册（原实现硬编码 Scoped，与注释「保持原生命周期」不符）。
    /// </remarks>
    private static IServiceCollection Decorate<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService, TService> decorate)
        where TService : class
    {
        var index = -1;
        ServiceDescriptor? existing = null;
        for (var i = 0; i < services.Count; i++)
        {
            if (services[i].ServiceType == typeof(TService))
            {
                index = i;
                existing = services[i];
            }
        }

        if (existing is null || index < 0)
        {
            return services;
        }

        var captured = existing;
        services[index] = ServiceDescriptor.Describe(
            typeof(TService),
            serviceProvider =>
            {
                var inner = BuildInner<TService>(captured, serviceProvider);
                return decorate(serviceProvider, inner);
            },
            captured.Lifetime);

        return services;
    }

    /// <summary>按原始注册构建被装饰的实例，并对 type-based 注册做确定性的构造函数选择。</summary>
    private static TService BuildInner<TService>(ServiceDescriptor descriptor, IServiceProvider serviceProvider)
        where TService : class
    {
        if (descriptor.ImplementationInstance is TService instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return descriptor.ImplementationFactory.Invoke(serviceProvider) as TService
                ?? throw new InvalidOperationException(
                    $"The factory registered for {typeof(TService).Name} returned null.");
        }

        var implementationType = descriptor.ImplementationType
            ?? throw new InvalidOperationException($"No implementation to decorate for {typeof(TService).Name}.");

        var constructor = SelectConstructor(implementationType, serviceProvider)
            ?? throw new InvalidOperationException(
                $"No public constructor of {implementationType.FullName} can be satisfied from the container; " +
                $"cannot decorate {typeof(TService).Name}.");

        var arguments = new object?[constructor.GetParameters().Length];
        var parameters = constructor.GetParameters();
        for (var i = 0; i < parameters.Length; i++)
        {
            arguments[i] = TryResolve(serviceProvider, parameters[i].ParameterType);
        }

        return (TService)constructor.Invoke(arguments);
    }

    /// <summary>
    /// 安全解析：<see cref="IServiceProvider.GetService"/> 对**开放泛型**（如 <c>ILogger&lt;&gt;</c>）
    /// 和破损注册是**抛异常**而不是返回 null，直接调用会让构造函数探测在启动阶段崩掉。
    /// 探测语义下「解析抛错」等价于「不可满足」。
    /// </summary>
    private static object? TryResolve(IServiceProvider serviceProvider, Type serviceType)
    {
        try
        {
            return serviceProvider.GetService(serviceType);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 确定性构造函数选择：优先 [ActivatorUtilitiesConstructor]，否则取「参数全可解析」中参数最多的一个；
    /// 仍并列时按参数个数和类型名排序取第一个（消除歧义，不用抛异常）。
    /// </summary>
    private static ConstructorInfo? SelectConstructor(Type implementationType, IServiceProvider serviceProvider)
    {
        var candidates = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (candidates.Length == 0)
        {
            return null;
        }

        var marked = candidates.FirstOrDefault(
            c => c.GetCustomAttribute<ActivatorUtilitiesConstructorAttribute>() is not null);
        if (marked is not null && CanSatisfy(marked, serviceProvider))
        {
            return marked;
        }

        return candidates
            .Where(c => CanSatisfy(c, serviceProvider))
            .OrderByDescending(c => c.GetParameters().Length)
            .ThenBy(c => string.Join(",", c.GetParameters().Select(p => p.ParameterType.FullName)), StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static bool CanSatisfy(ConstructorInfo constructor, IServiceProvider serviceProvider)
    {
        foreach (var parameter in constructor.GetParameters())
        {
            if (parameter.HasDefaultValue)
            {
                continue;
            }

            // 用 TryResolve 而非 GetService：开放泛型/破损注册会抛异常，探测语义下视为不可满足
            if (TryResolve(serviceProvider, parameter.ParameterType) is null)
            {
                return false;
            }
        }

        return true;
    }
}
