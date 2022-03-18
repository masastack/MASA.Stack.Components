namespace Masa.Stack.Components;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMasaStackComponentsForServer(this IServiceCollection services, string i18nDirectoryPath)
    {
        services.AddMasaI18nForServer(i18nDirectoryPath);

        services.AddScoped<GlobalConfig>();

        return services;
    }

    public static IServiceCollection AddMasaStackComponentsForWasm(this IServiceCollection services, string i18nDirectoryPath)
    {
        _ = services.AddMasaI18nForWasmAsync(i18nDirectoryPath);

        services.AddScoped<GlobalConfig>();

        return services;
    }
}