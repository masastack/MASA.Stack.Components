using Masa.Stack.Components.Infrastructure.Impersonation;

namespace Masa.Stack.Components;

public static class ServiceCollectionExtensions
{
    public static void AddMasaStackComponent(this IServiceCollection services, MasaStackComponentOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Project);
        ArgumentNullException.ThrowIfNull(options.Project.Name);

        // SLayout 内嵌了 ImpersonationBanner，而横幅要注入 ImpersonationSession。
        // 这里用 TryAdd 兜底，保证只用 SLayout、不走 AddMasaOpenIdConnectAsync 的宿主（Server 模式、
        // 自建认证装配）也不会因为缺注册而在渲染布局时抛 InvalidOperationException。
        services.TryAddScoped<ImpersonationSession>();

        services.TryAddScoped<CookieStorage>();
        services.TryAddScoped<LocalStorage>();
        services.TryAddScoped<JsInitVariables>();
        services.AddScoped(sp => new SkinManager(
            sp.GetRequiredService<MasaBlazor>(),
            sp.GetRequiredService<IJSRuntime>(),
            sp.GetRequiredService<GlobalConfig>(),
            options.SkinCatalog,
            options.IsMicroFrontend));

        // 全局为所有 HttpClient 自动附加 Accept-Language（语言取自界面 scope 的 I18n）
        services.AddAcceptLanguageHandler();

        // 注册团队状态管理器 - 根据运行时环境选择合适的实现
        services.AddScoped<ITeamStateManager>(sp =>
        {
            if (IsWebAssemblyEnvironment(sp))
            {
                var navigationManager = sp.GetRequiredService<NavigationManager>();
                var jsRuntime = sp.GetRequiredService<IJSRuntime>();
                var logger = sp.GetRequiredService<ILogger<WasmTeamStateManager>>();
                return new WasmTeamStateManager(navigationManager, jsRuntime, logger);
            }
            else
            {
                var authStateManager = sp.GetRequiredService<AuthenticationStateManager>();
                var authStateProvider = sp.GetRequiredService<AuthenticationStateProvider>();
                return new ServerTeamStateManager(authStateManager, authStateProvider);
            }
        });

        services.AddAutoInject();
        services.AddMemoryCache();
        services.AddMasaIdentity(options =>
        {
            options.UserName = IdentityClaimConsts.USER_NAME;
            options.UserId = IdentityClaimConsts.USER_ID;
            options.Role = IdentityClaimConsts.ROLES;
            options.Environment = IdentityClaimConsts.ENVIRONMENT;
            options.Mapping(nameof(MasaUser.CurrentTeamId), IdentityClaimConsts.CURRENT_TEAM);
            options.Mapping(nameof(MasaUser.StaffId), IdentityClaimConsts.STAFF);
            options.Mapping(nameof(MasaUser.Account), IdentityClaimConsts.ACCOUNT);
            options.Mapping(nameof(MasaUser.PhoneNumber), IdentityClaimConsts.PHONE_NUMBER);
            options.Mapping(nameof(MasaUser.Email), IdentityClaimConsts.EMAIL);
        });
        services.AddSingleton(sp => new ProjectAppOptions(options.Project, options.ServiceVersion));
        services.AddScoped<MiniProgramAccessState>();
        services.AddScoped<MiniProgramGuard>();

        var masaStackConfig = services.GetMasaStackConfig();

        options.DccHost ??= masaStackConfig.GetDccServiceDomain();
        options.AuthHost ??= masaStackConfig.GetAuthServiceDomain();
        options.McHost ??= masaStackConfig.GetMcServiceDomain();
        options.PmHost ??= masaStackConfig.GetPmServiceDomain();
        options.SappHost ??= masaStackConfig.GetSuperAppServiceDomain();

        services.AddDccClient(options.DccHost);
        services.AddAuthClient(options.AuthHost);
        services.AddSingleton(new McServiceOptions(options.McHost));
        services.AddMcClient(options.McHost);
        services.AddPmClient(options.PmHost);
        services.AddSappClient(options.SappHost);

        services.AddScoped((serviceProvider) =>
        {
            var masaUser = serviceProvider.GetRequiredService<IUserContext>().GetUser<MasaUser>() ?? new MasaUser();
            return masaUser;
        });

        services.AddScoped(sp =>
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            return new MicroFrontendNavigationManager(navigationManager, options.IsMicroFrontend ? $"/{options.Project.Name.ToLower()}/" : "/");
        });

        var masaBuilder = services.AddMasaBlazor(masaBlazorOptions =>
        {
            masaBlazorOptions.ConfigureTheme(theme =>
            {
                theme.Themes.Light.Primary = "#4318FF";
                theme.Themes.Light.Accent = "#006c4f";
                theme.Themes.Light.Error = "#ba1a1a";
                theme.Themes.Light.Success = "#00B42A";
                theme.Themes.Light.Warning = "#FF5252";
                theme.Themes.Light.Info = "#37A7FF";
                theme.Themes.Light.Surface = "#F0F3FA";
                theme.Themes.Light.Secondary = "#424242";
                //theme.Themes.Light.UserDefined["reminder"] = "#FF7D00";
                //theme.Themes.Light.Variables.BorderOpacity = 1f;

            });
            masaBlazorOptions.Defaults = new Dictionary<string, IDictionary<string, object?>?>
            {
                {
                    PopupComponents.SNACKBAR, new Dictionary<string, object?>
                    {
                        { nameof(PEnqueuedSnackbars.Position), SnackPosition.BottomCenter }
                    }
                }
            };
            options.MasaBlazorOptionsAction?.Invoke(masaBlazorOptions);
        })
        .AddI18n(GetLocales().ToArray());

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), ServiceLifetime.Scoped, null, includeInternalTypes: true);

        if (options.I18nDirectoryPath is not null)
        {
            // 加载任务需要在 InitializeMasaStackApplicationAsync 中等待完成,避免首屏渲染时多语言还未就绪
            var loadingTask = masaBuilder.AddI18nForWasmAsync(options.I18nDirectoryPath);
            services.AddSingleton(new I18nLoadingState(loadingTask));
        }

        options.MasaBlazorAction?.Invoke(masaBuilder);
    }

    /// <summary>
    /// 注册 <see cref="AcceptLanguageHandler"/>，并通过 <see cref="IHttpMessageHandlerBuilderFilter"/>
    /// 把它挂到所有由 <see cref="IHttpClientFactory"/> 创建的 HttpClient 管线上
    /// （.NET 6/7 没有 ConfigureHttpClientDefaults，用 filter 实现相同效果）。
    /// </summary>
    internal static IServiceCollection AddAcceptLanguageHandler(this IServiceCollection services)
    {
        // 必须是 Transient：
        // - DelegatingHandler 的 InnerHandler 只能设置一次，同一个实例不能被多条管线共享，所以不能是 Singleton；
        // - 也不要用 Scoped：handler 是在 IHttpClientFactory 自己新建的 scope 里解析的，
        //   那个 scope 里的 Scoped 服务（如 I18n）与界面 scope 毫无关系，所以 handler 自己也不能依赖它们
        //   （界面语言由 AcceptLanguageHandler 通过 IClientScopeServiceProviderAccessor 读取）。
        services.TryAddTransient<AcceptLanguageHandler>();

        // TryAddEnumerable：重复调用 AddMasaStackComponent 时 filter 也只挂一次
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, AcceptLanguageHandlerFilter>());

        return services;
    }

    private static IEnumerable<(string cultureName, Dictionary<string, string> map)> GetLocales()
    {
        var output = new List<(string cultureName, Dictionary<string, string> map)>();
        var assembly = typeof(ServiceCollectionExtensions).Assembly;
        var availableResources = assembly.GetManifestResourceNames()
                                         .Select(s => Regex.Match(s, @"^.*Locales\.(.+)\.json"))
                                         .Where(s => s.Success && s.Groups[1].Value != "supportedCultures")
                                         .ToDictionary(s => s.Groups[1].Value, s => s.Value);
        foreach (var (cultureName, fileName) in availableResources)
        {
            using var fileStream = assembly.GetManifestResourceStream(fileName);
            if (fileStream is not null)
            {
                using var streamReader = new StreamReader(fileStream);
                var content = streamReader.ReadToEnd();
                var locale = I18nReader.Read(content);
                output.Add((cultureName, locale));
            }
        }
        return output;
    }

    /// <summary>
    /// 检测是否为 WebAssembly 环境
    /// </summary>
    private static bool IsWebAssemblyEnvironment(IServiceProvider serviceProvider)
    {
        try
        {
            // 在 WASM 模式下，IJSRuntime 实现了 IJSInProcessRuntime 接口
            // 在 Server 模式下，由于网络延迟，JS 调用都是异步的，不会实现 IJSInProcessRuntime
            var jsRuntime = serviceProvider.GetService<IJSRuntime>();
            return jsRuntime is IJSInProcessRuntime;
        }
        catch
        {
            // 如果出现异常，默认为 Server 模式
            return false;
        }
    }

    private static async Task WaitForI18nLoadingAsync(IServiceProvider serviceProvider)
    {
        var state = serviceProvider.GetService<I18nLoadingState>();
        if (state is null)
        {
            return;
        }

        try
        {
            await state.LoadingTask;
        }
        catch (Exception ex)
        {
            // 与之前 fire-and-forget 的行为保持一致:多语言加载失败不阻塞应用启动,只记录日志
            serviceProvider.GetService<ILoggerFactory>()
                           ?.CreateLogger(typeof(ServiceCollectionExtensions))
                           .LogError(ex, "Failed to load i18n files from I18nDirectoryPath");
        }
    }

    public static async Task InitializeMasaStackApplicationAsync(
        [NotNull] this IServiceProvider serviceProvider)
    {
        // 必须先等待多语言文件加载完成,再创建 I18n 实例(I18n 会在构造时对 Locale 取快照)
        await WaitForI18nLoadingAsync(serviceProvider);

        await serviceProvider.InitializeApplicationAsync();
        await serviceProvider.GetRequiredService<IClientScopeServiceProviderAccessor>().ServiceProvider.GetRequiredService<MasaStackConfigCache>().InitializeAsync();
        var IMasaStackConfig = serviceProvider.GetRequiredService<IMasaStackConfig>();
        var IMultiEnvironmentMasaStackConfig = serviceProvider.GetRequiredService<IMultiEnvironmentMasaStackConfig>();
        await serviceProvider.GetRequiredService<IClientScopeServiceProviderAccessor>().ServiceProvider.GetRequiredService<I18nCache>().InitializeAsync();
    }
}
