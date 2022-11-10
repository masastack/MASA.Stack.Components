namespace Masa.Stack.Components;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMasaStackComponentsForServer(this WebApplicationBuilder builder,
        string? i18nDirectoryPath = "wwwroot/i18n", string? authHost = null, string? mcHost = null, string? pmHost = null)
    {
        builder.Services.AddMasaConfiguration(configurationBuilder =>
        {
            configurationBuilder.UseDcc();
        });
        var publicConfiguration = builder.Services.GetMasaConfiguration().ConfigurationApi.GetPublic();
        authHost = authHost ?? publicConfiguration.GetValue<string>("$public.AppSettings:AuthClient:Url");
        mcHost = mcHost ?? publicConfiguration.GetValue<string>("$public.AppSettings:McClient:Url");
        pmHost = pmHost ?? publicConfiguration.GetValue<string>("$public.AppSettings:PmClient:Url");

        UserAutoCompleteOptions userAutoCompleteOptions = publicConfiguration.GetSection("$public.ES.UserAutoComplete").Get<UserAutoCompleteOptions>();
        RedisConfigurationOptions redisOption = publicConfiguration.GetSection("$public.RedisConfig").Get<RedisConfigurationOptions>();

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
        builder.Services.AddScoped((serviceProvider) =>
        {
            var masaUser = serviceProvider.GetRequiredService<IUserContext>().GetUser<MasaUser>() ?? new MasaUser();
            return masaUser;
        });
        builder.Services.AddAuthClient(authHost, redisOption);
        var options = new McServiceOptions(mcHost);
        builder.Services.AddSingleton(options);
        builder.Services.AddMcClient(mcHost);
        builder.Services.AddPmClient(pmHost);

        var masaBuilder = builder.Services.AddMasaBlazor(builder =>
        {
            builder.ConfigureTheme(theme =>
            {
                theme.Themes.Light.Primary = "#4318FF";
                theme.Themes.Light.Accent = "#4318FF";
                theme.Themes.Light.Error = "#FF5252";
                theme.Themes.Light.Success = "#00B42A";
                theme.Themes.Light.Warning = "#FF7D00";
                theme.Themes.Light.Info = "#37A7FF";
            });
        })
        .AddI18n(GetLocales().ToArray());

        if (i18nDirectoryPath is not null)
        {
            masaBuilder.AddI18nForServer(i18nDirectoryPath);
        }
        builder.Services.AddOss();
        builder.Services.AddElasticsearchAutoComplete(userAutoCompleteOptions);

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
