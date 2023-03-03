namespace Masa.Stack.Components;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMasaStackComponentsForServer(this WebApplicationBuilder builder,
        string? i18nDirectoryPath = "wwwroot/i18n", string? authHost = null, string? mcHost = null,
        string? pmHost = null, RedisConfigurationOptions? redisOption = null)
    {
        builder.Services.AddScoped<JsInitVariables>();
        builder.Services.AddAutoInject();
        builder.Services.AddMasaIdentity(options =>
        {
            options.UserName = "name";
            options.UserId = "sub";
            options.Role = IdentityClaimConsts.ROLES;
            options.Environment = IdentityClaimConsts.ENVIRONMENT;
            options.Mapping(nameof(MasaUser.CurrentTeamId), IdentityClaimConsts.CURRENT_TEAM);
            options.Mapping(nameof(MasaUser.StaffId), IdentityClaimConsts.STAFF);
            options.Mapping(nameof(MasaUser.Account), IdentityClaimConsts.ACCOUNT);
            options.Mapping(nameof(MasaUser.PhoneNumber), IdentityClaimConsts.PHONE_NUMBER);
            options.Mapping(nameof(MasaUser.Email), IdentityClaimConsts.EMAIL);
        });

        builder.Services.AddMasaStackConfigAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        var masaStackConfig = builder.Services.GetMasaStackConfig();

        //temporary compatible
        if (masaStackConfig.IsDemo)
        {
            var publicConfiguration = builder.Services.GetMasaConfiguration().ConfigurationApi.GetPublic();
            authHost = masaStackConfig.GetAuthServiceDomain();
            mcHost = masaStackConfig.GetMcServiceDomain();
            pmHost = masaStackConfig.GetPmServiceDomain();
            redisOption = new RedisConfigurationOptions
            {
                Servers = new List<RedisServerOptions> {
                    new RedisServerOptions()
                    {
                        Host= masaStackConfig.RedisModel.RedisHost,
                        Port= masaStackConfig.RedisModel.RedisPort
                    }
                },
                DefaultDatabase = masaStackConfig.RedisModel.RedisDb,
                Password = masaStackConfig.RedisModel.RedisPassword
            };

            builder.Services.AddAuthClient(authHost, redisOption);
            var options = new McServiceOptions(() =>
            {
                return masaStackConfig.GetMcServiceDomain();
            });
            builder.Services.AddSingleton(options);
            builder.Services.AddMcClient(mcHost);
            builder.Services.AddPmClient(pmHost);

            builder.Services.AddOss();
            builder.Services.AddElasticsearchAutoComplete(() =>
            {
                return new UserAutoCompleteOptions
                {
                    Index = masaStackConfig.ElasticModel.Index,
                    Nodes = new string[1] { $"{masaStackConfig.ElasticModel.ESNode}:{masaStackConfig.ElasticModel.ESPort}" }
                };
            });
        }
        else
        {
            var publicConfiguration = builder.Services.GetMasaConfiguration().ConfigurationApi.GetPublic();
            authHost = authHost ?? publicConfiguration.GetValue<string>("$public.AppSettings:AuthClient:Url");
            redisOption = redisOption ?? publicConfiguration.GetSection("$public.RedisConfig").Get<RedisConfigurationOptions>();

            builder.Services.AddAuthClient(authHost, redisOption);

            var cluster = builder.Services.GetLocalConfiguration("DccOptions").GetValue<string>("Cluster");
            var appId = "public-$Config";
            var configurationApiClient = builder.Services.BuildServiceProvider().GetRequiredService<IConfigurationApiClient>();

            var options = new McServiceOptions(() =>
            {
                var envirment = builder.Services.BuildServiceProvider().GetRequiredService<IEnvironmentProvider>().GetEnvironment();
                var appSettings = configurationApiClient.GetDynamicAsync(envirment, cluster, appId, "$public.AppSettings").ConfigureAwait(false).GetAwaiter().GetResult();
                return mcHost ?? appSettings.McClient.Url;
            });
            builder.Services.AddSingleton(options);
            builder.Services.AddMcClient(() =>
            {
                var envirment = builder.Services.BuildServiceProvider().GetRequiredService<IEnvironmentProvider>().GetEnvironment();
                var appSettings = configurationApiClient.GetDynamicAsync(envirment, cluster, appId, "$public.AppSettings").ConfigureAwait(false).GetAwaiter().GetResult();
                return mcHost ?? appSettings.McClient.Url;
            });
            builder.Services.AddPmClient(() =>
            {
                var envirment = builder.Services.BuildServiceProvider().GetRequiredService<IEnvironmentProvider>().GetEnvironment();
                var appSettings = configurationApiClient.GetDynamicAsync(envirment, cluster, appId, "$public.AppSettings").ConfigureAwait(false).GetAwaiter().GetResult();
                return pmHost ?? appSettings.PmClient.Url;
            });

            builder.Services.AddOss();
            builder.Services.AddElasticsearchAutoComplete(() =>
            {
                var envirment = builder.Services.BuildServiceProvider()
                    .GetRequiredService<IEnvironmentProvider>().GetEnvironment();
                var userAutoCompleteOptions = configurationApiClient
                    .GetAsync<UserAutoCompleteOptions>(envirment, cluster, appId, "$public.ES.UserAutoComplete", (_) =>
                    {
                        var autoCompleteFactory = builder.Services.BuildServiceProvider().GetRequiredService<IAutoCompleteFactory>();
                        var autoCompleteClient = autoCompleteFactory.Create();
                        autoCompleteClient.BuildAsync();
                    }).ConfigureAwait(false).GetAwaiter().GetResult();

                return userAutoCompleteOptions;
            });
        }

        builder.Services.AddScoped<TokenProvider>();
        builder.Services.AddScoped((serviceProvider) =>
        {
            var masaUser = serviceProvider.GetRequiredService<IUserContext>().GetUser<MasaUser>() ?? new MasaUser();
            return masaUser;
        });

        var masaBuilder = builder.Services.AddMasaBlazor(options =>
        {
            options.ConfigureTheme(theme =>
            {
                theme.Themes.Light.Primary = "#4318FF";
                theme.Themes.Light.Accent = "#4318FF";
                theme.Themes.Light.Error = "#FF5252";
                theme.Themes.Light.Success = "#00B42A";
                theme.Themes.Light.Warning = "#FF7D00";
                theme.Themes.Light.Info = "#37A7FF";
            });
            options.Defaults = new Dictionary<string, IDictionary<string, object?>?>()
            {
                {
                    PopupComponents.SNACKBAR, new Dictionary<string, object?>()
                    {
                        { nameof(PEnqueuedSnackbars.Position), SnackPosition.BottomCenter }
                    }
                }
            };
        })
        .AddI18n(GetLocales().ToArray());

        if (i18nDirectoryPath is not null)
        {
            masaBuilder.AddI18nForServer(i18nDirectoryPath);
        }

        return builder.Services;
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
}
