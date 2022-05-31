﻿namespace Masa.Stack.Components;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMasaStackComponentsForServer(this IServiceCollection services, string i18nDirectoryPath)
    {
        services.AddMasaI18nForServer(i18nDirectoryPath);
        services.AddMasaBlazor(builder =>
        {
            builder.UseTheme(option =>
            {
                option.Primary = "#4318FF";
                option.Accent = "#4318FF";
                option.Error = "#FF5252";
                option.Success = "#00B42A";
                option.Warning = "#FF7D00";
                option.Info = "#37A7FF";
            });
        });
        services.AddScoped<JsInterop.JsDotNetInvoker>();
        services.AddScoped<GlobalConfig>();

        return services;
    }

    public static async Task<IServiceCollection> AddMasaStackComponentsForWasmAsync(this IServiceCollection services, string i18nDirectoryPath)
    {
        await services.AddMasaI18nForWasmAsync(i18nDirectoryPath);
        services.AddMasaBlazor(builder =>
        {
            builder.UseTheme(option =>
            {
                option.Primary = "#4318FF";
                option.Accent = "#4318FF";
                option.Error = "#FF5252";
                option.Success = "#00B42A";
                option.Warning = "#FF7D00";
                option.Info = "#37A7FF";
            });
        });
        services.AddScoped<JsInterop.JsDotNetInvoker>();
        services.AddScoped<GlobalConfig>();

        return services;
    }
}