﻿namespace Masa.Stack.Components;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMasaStackComponentsForServer(this IServiceCollection services,
        string i18nDirectoryPath, string authHost, string mcHost)
    {
        services.AddAuthClient(authHost);
        var options = new McApiOptions(mcHost);
        services.AddSingleton(options);
        services.AddMcClient(mcHost);
        services.AddScoped<NoticeState>();

        services.AddMasaBlazor(builder =>
        {
            builder.Theme.Primary = "#4318FF";
            builder.Theme.Accent = "#4318FF";
            builder.Theme.Error = "#FF5252";
            builder.Theme.Success = "#00B42A";
            builder.Theme.Warning = "#FF7D00";
            builder.Theme.Info = "#37A7FF";
        }).AddI18nForServer(i18nDirectoryPath);
        services.AddScoped<JsInterop.JsDotNetInvoker>();
        services.AddScoped<GlobalConfig>();

        return services;
    }

    public static async Task<IServiceCollection> AddMasaStackComponentsForWasmAsync(this IServiceCollection services,
        string i18nDirectoryPath, string authHost, string mcHost)
    {
        services.AddAuthClient(authHost);
        var options = new McApiOptions(mcHost);
        services.AddSingleton(options);
        services.AddMcClient(mcHost);
        services.AddScoped<NoticeState>();

        services.AddMasaBlazor(builder =>
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
}
