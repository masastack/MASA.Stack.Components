namespace Masa.Stack.Components;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMasaStackComponentsForServer(this IServiceCollection services,
        string? i18nDirectoryPath, string authHost, string mcHost)
    {
        services.AddSingleton<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
        services.AddSingleton<ICurrentPrincipalAccessor, BlazorCurrentPrincipalAccessor>();

        services.AddMasaIdentityModel(IdentityType.MultiEnvironment, options =>
        {
            options.Environment = "environment";
            options.UserName = "name";
            options.UserId = "sub";
            options.Role = "role";
        });

        services.AddAuthClient(authHost);
        var options = new McServiceOptions(mcHost);
        services.AddSingleton(options);
        services.AddMcClient(mcHost);
        services.AddScoped<NoticeState>();

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

        if(i18nDirectoryPath is not null) builder.AddI18nForServer(i18nDirectoryPath);
        services.AddScoped<JsInterop.JsDotNetInvoker>();
        services.AddScoped<GlobalConfig>();

        return services;
    }

    public static async Task<IServiceCollection> AddMasaStackComponentsForWasmAsync(this IServiceCollection services,
        string i18nDirectoryPath, string authHost, string mcHost)
    {
        services.AddAuthClient(authHost);
        var options = new McServiceOptions(mcHost);
        services.AddSingleton(options);
        services.AddMcClient(mcHost);
        services.AddScoped<NoticeState>();

        await services.AddMasaBlazor(builder =>
        {
            builder.Theme.Primary = "#4318FF";
            builder.Theme.Accent = "#4318FF";
            builder.Theme.Error = "#FF5252";
            builder.Theme.Success = "#00B42A";
            builder.Theme.Warning = "#FF7D00";
            builder.Theme.Info = "#37A7FF";
        }).AddI18nForWasmAsync(i18nDirectoryPath);
        services.AddScoped<JsInterop.JsDotNetInvoker>();
        services.AddScoped<GlobalConfig>();

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
        foreach(var (cultureName, fileName) in availableResources)
        {
            using var fileStream = assembly.GetManifestResourceStream(fileName);
            if(fileStream is not null)
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
