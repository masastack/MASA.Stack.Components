using Masa.BuildingBlocks.Authentication.Identity;
using Masa.BuildingBlocks.StackSdks.Auth.Contracts;
using Masa.BuildingBlocks.StackSdks.Auth.Contracts.Consts;

namespace Masa.Stack.Components;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMasaStackComponentsForServer(this WebApplicationBuilder builder,
        string? i18nDirectoryPath = "wwwroot/i18n", string? authHost = null, string? mcHost = null)
    {
        builder.AddMasaConfiguration(configurationBuilder =>
        {
            configurationBuilder.UseDcc();
        });
        var publicConfiguration = builder.GetMasaConfiguration().ConfigurationApi.GetPublic();
        builder.Services.AddMasaStackComponentsForServer(
                i18nDirectoryPath,
                authHost ?? publicConfiguration.GetValue<string>("$public.AppSettings:AuthClient:Url"),
                mcHost ?? publicConfiguration.GetValue<string>("$public.AppSettings:McClient:Url"),
                publicConfiguration.GetSection("$public.OSS").Get<OssOptions>(),
                publicConfiguration.GetSection("$public.ES.UserAutoComplete").Get<UserAutoCompleteOptions>()
            );

        return builder.Services;
    }

    public static IServiceCollection AddMasaStackComponentsForServer(this IServiceCollection services,
       string? i18nDirectoryPath, string authHost, string mcHost, OssOptions ossOptions, UserAutoCompleteOptions userAutoCompleteOptions)
    {
        services.AddAutoInject();
        services.AddSingleton<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
        services.AddSingleton<ICurrentPrincipalAccessor, BlazorCurrentPrincipalAccessor>();

        services.AddMasaIdentityModel(options =>
        {
            options.UserName = "name";
            options.UserId = "sub";
            options.Role = IdentityClaimConsts.ROLES;
            options.Environment = IdentityClaimConsts.ENVIRONMENT;
            options.Mapping(nameof(MasaUser.CurrentTeamId), IdentityClaimConsts.CURRENT_TEAM);
            options.Mapping(nameof(MasaUser.StaffId), IdentityClaimConsts.STAFF);
            options.Mapping(nameof(MasaUser.Account), IdentityClaimConsts.ACCOUNT);
        });
        services.AddScoped((serviceProvider) =>
        {
            var userSetter = serviceProvider.GetRequiredService<IUserSetter>();
            var masaUser = serviceProvider.GetRequiredService<IUserContext>().GetUser<MasaUser>() ?? new MasaUser();
            return masaUser;
        });
        services.AddAuthClient(authHost);
        var options = new McServiceOptions(mcHost);
        services.AddSingleton(options);
        services.AddMcClient(mcHost);

        var builder = services.AddMasaBlazor(builder =>
        {
            builder.Theme.Primary = "#4318FF";
            builder.Theme.Accent = "#4318FF";
            builder.Theme.Error = "#FF5252";
            builder.Theme.Success = "#00B42A";
            builder.Theme.Warning = "#FF7D00";
            builder.Theme.Info = "#37A7FF";
        })
        .AddI18n(GetLocales().ToArray());

        if (i18nDirectoryPath is not null) builder.AddI18nForServer(i18nDirectoryPath);
        builder.Services.AddOss(ossOptions);
        builder.Services.AddElasticsearchAutoComplete(userAutoCompleteOptions);

        return services;
    }

    public static async Task<IServiceCollection> AddMasaStackComponentsForWasmAsync(this IServiceCollection services,
        string i18nDirectoryPath, string authHost, string mcHost)
    {
        services.AddAuthClient(authHost);
        var options = new McServiceOptions(mcHost);
        services.AddSingleton(options);
        services.AddMcClient(mcHost);

        await services.AddMasaBlazor(builder =>
        {
            builder.Theme.Primary = "#4318FF";
            builder.Theme.Accent = "#4318FF";
            builder.Theme.Error = "#FF5252";
            builder.Theme.Success = "#00B42A";
            builder.Theme.Warning = "#FF7D00";
            builder.Theme.Info = "#37A7FF";
        }).AddI18nForWasmAsync(i18nDirectoryPath);

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
}
