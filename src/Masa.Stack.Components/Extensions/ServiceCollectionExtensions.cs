namespace Masa.Stack.Components;

public static class ServiceCollectionExtensions
{
    //Consider only one web clearance for now
    public static IServiceCollection AddMasaStackComponent(this IServiceCollection services, MasaStackProject project,
        string? i18nDirectoryPath = "wwwroot/i18n",
        string? authHost = null, string? mcHost = null, string? pmHost = null, bool microFrontend = false, Action<IMasaBlazorBuilder>? masaBalazorAction = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        AddMasaStackComponentsService(services, project, i18nDirectoryPath, authHost, mcHost, pmHost, microFrontend: microFrontend, masaBalazorAction: masaBalazorAction);
        AddObservable(services, true, project);
        return services;
    }

    public static IServiceCollection AddMasaStackComponentsWithNormalApp(this IServiceCollection services, MasaStackProject project, string otlpUrl, string serviceVersion, string projectName = "default",
        string? i18nDirectoryPath = "wwwroot/i18n",
        string? authHost = null, string? mcHost = null, string? pmHost = null, bool microFrontend = false,
         Action<IMasaBlazorBuilder>? masaBalazorAction = default)
    {
        ArgumentNullException.ThrowIfNull(projectName);
        ArgumentNullException.ThrowIfNull(otlpUrl);
        ArgumentNullException.ThrowIfNull(serviceVersion);
        ArgumentNullException.ThrowIfNull(projectName);
        AddMasaStackComponentsService(services, project, i18nDirectoryPath, authHost, mcHost, pmHost, serviceVersion, microFrontend, masaBalazorAction);
        AddObservable(services, false, project: project, serviceVersion: serviceVersion, projectName: projectName, otlpUrl: otlpUrl);
        return services;
    }

    private static void AddMasaStackComponentsService(IServiceCollection services, MasaStackProject project,
        string? i18nDirectoryPath = "wwwroot/i18n",
        string? authHost = null, string? mcHost = null, string? pmHost = null, string? serviceVersion = null, bool microFrontend = false,
        Action<IMasaBlazorBuilder>? masaBalazorAction = default)
    {
        services.TryAddScoped<CookieStorage>();
        services.TryAddScoped<LocalStorage>();
        services.TryAddScoped<JsInitVariables>();

        // 注册团队状态管理器 - 根据运行时环境选择合适的实现
        services.AddScoped<ITeamStateManager>(sp =>
        {
            // 检测是否为 WASM 模式
            if (IsWebAssemblyEnvironment(sp))
            {
                // WASM 模式：使用 refresh token 获取最新的 token 和 claims
                var authStateProvider = sp.GetRequiredService<AuthenticationStateProvider>();
                var navigationManager = sp.GetRequiredService<NavigationManager>();
                var tokenProvider = sp.GetRequiredService<IAccessTokenProvider>();
                var logger = sp.GetRequiredService<ILogger<WasmTeamStateManager>>();
                return new WasmTeamStateManager(authStateProvider, navigationManager, tokenProvider, logger);
            }
            else
            {
                // Server 模式：直接操作身份验证状态
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
        services.AddSingleton(sp => new ProjectAppOptions(project, serviceVersion));

        var masaStackConfig = services.GetMasaStackConfig();

        var dccHost = masaStackConfig.GetDccServiceDomain();
        authHost ??= masaStackConfig.GetAuthServiceDomain();
        mcHost ??= masaStackConfig.GetMcServiceDomain();
        pmHost ??= masaStackConfig.GetPmServiceDomain();
        services.AddDccClient(dccHost);
        services.AddAuthClient(authHost);
        services.AddSingleton(new McServiceOptions(mcHost));
        services.AddMcClient(mcHost);
        services.AddPmClient(pmHost);

        services.AddScoped((serviceProvider) =>
        {
            var masaUser = serviceProvider.GetRequiredService<IUserContext>().GetUser<MasaUser>() ?? new MasaUser();
            return masaUser;
        });

        services.AddScoped(sp =>
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            return new MicroFrontendNavigationManager(navigationManager, microFrontend ? $"/{project.Name.ToLower()}/" : "/");
        });

        var masaBuilder = services.AddMasaBlazor(options =>
        {
            options.ConfigureTheme(theme =>
            {
                theme.Themes.Light.Primary = "#4318FF";
                theme.Themes.Light.Accent = "#4318FF";
                theme.Themes.Light.Error = "#FF5252";
                theme.Themes.Light.Success = "#00B42A";
                theme.Themes.Light.Warning = "#FF7D00";
                theme.Themes.Light.Info = "#37A7FF";
                theme.Themes.Light.Surface = "#F0F3FA";
            });
            options.Defaults = new Dictionary<string, IDictionary<string, object?>?>
            {
                {
                    PopupComponents.SNACKBAR, new Dictionary<string, object?>
                    {
                        { nameof(PEnqueuedSnackbars.Position), SnackPosition.BottomCenter }
                    }
                }
            };
        })
        .AddI18n(GetLocales().ToArray());

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), ServiceLifetime.Scoped, null, includeInternalTypes: true);

        if (i18nDirectoryPath is not null)
        {
            masaBuilder.AddI18nForWasmAsync(i18nDirectoryPath);
        }

        masaBalazorAction?.Invoke(masaBuilder);
    }

    private static void AddObservable(IServiceCollection services, bool isMasa, MasaStackProject project, string? serviceVersion = null, string? projectName = null, string? otlpUrl = null)
    {
        services.AddLogging(configure =>
        {
            var masaStackConfig = services.GetMasaStackConfig();
            // 创建配置构建器
            //IConfiguration configuration = new ConfigurationBuilder()
            //    .AddEnvironmentVariables()
            //    .Build();

            //var masaSericeName = isMasa ? masaStackConfig.GetWebId(project) : project.Name;
            //ArgumentNullException.ThrowIfNull(masaSericeName);
            //services.AddObservable(configure, () => new MasaObservableOptions
            //{
            //    ServiceNameSpace = masaStackConfig.Environment,
            //    ServiceVersion = isMasa ? masaStackConfig.Version : serviceVersion!,
            //    ServiceName = masaSericeName,
            //    Layer = isMasa ? masaStackConfig.Namespace : projectName!,
            //    ServiceInstanceId = configuration.GetValue<string>("HOSTNAME")!
            //}, () => isMasa ? masaStackConfig.OtlpUrl : otlpUrl!, true);
        });
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

    public static async Task InitializeMasaStackApplicationAsync(
        [NotNull] this IServiceProvider serviceProvider)
    {
        await serviceProvider.InitializeApplicationAsync();
        await serviceProvider.GetRequiredService<IClientScopeServiceProviderAccessor>().ServiceProvider.GetRequiredService<MasaStackConfigCache>().InitializeAsync();
        var IMasaStackConfig = serviceProvider.GetRequiredService<IMasaStackConfig>();
        var IMultiEnvironmentMasaStackConfig = serviceProvider.GetRequiredService<IMultiEnvironmentMasaStackConfig>();
        await serviceProvider.GetRequiredService<IClientScopeServiceProviderAccessor>().ServiceProvider.GetRequiredService<I18nCache>().InitializeAsync();
    }
}
