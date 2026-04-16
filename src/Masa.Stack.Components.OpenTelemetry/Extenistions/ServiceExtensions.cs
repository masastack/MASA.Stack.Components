namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceExtensions
{
    public static IServiceCollection AddMasaBlazorWasmObservable(this IServiceCollection services, ILoggingBuilder loggingBuilder, MasaBlazorWasmObservableOptions options, string otelUrl, string? getClientIpUrl = null)
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_EXPERIMENTAL_HTTPCLIENT_DISABLE_URL_QUERY_REDACTION", "true");

        var resourceBuilder = GetResourceBuilder(options);
        if (!string.IsNullOrEmpty(getClientIpUrl))
            MasaBlazorActivityContent.GetIpUrl = getClientIpUrl;

        var traceProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource(MasaBlazorWasmConstants.MasaBlazorWasmActivitySourceName)
            .AddHttpClientInstrumentation(config =>
            {
                config.FilterHttpRequestMessage = (httpRequest) => HttpRequestMessageInstrumentation.FilterHttpRequest(httpRequest, otelUrl);
                config.EnrichWithHttpRequestMessage = HttpRequestMessageInstrumentation.OnHttpMessageRequest;
                config.EnrichWithHttpResponseMessage = HttpRequestMessageInstrumentation.OnHttpResponseMessage;
                config.EnrichWithException = (activity, exception) =>
                {
                    activity.SetTag(MasaBlazorWasmConstants.ExceptionType, exception.GetType().FullName);
                    activity.SetTag(MasaBlazorWasmConstants.ExceptionMessage, exception.Message);
                };
            })
            .AddProcessor(new SimpleActivityExportProcessor(new WasmTraceExporter(new HttpClient(),
            HttpRequestMessageInstrumentation.GetOtelUrl(otelUrl, isTrace: true))))
            .Build();

        services.AddSingleton(traceProvider);

        loggingBuilder.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resourceBuilder);
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;

            options.AddProcessor(new SimpleLogRecordExportProcessor(new WasmLogExporter(new HttpClient(),
                HttpRequestMessageInstrumentation.GetOtelUrl(otelUrl, isLog: true))));
        });

        _ = Task.Factory.StartNew(() =>
        {
            HttpRequestMessageInstrumentation.Logger = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("Masa.Stack.Components.OpenTelemetry");
            RouteUtils.LoadRoutes();
        });

        return services;
    }

    private static ResourceBuilder GetResourceBuilder(MasaBlazorWasmObservableOptions options)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(options.ServiceName, options.ServiceNameSpace, options.ServiceVersion, true);

        if (!string.IsNullOrEmpty(options.ProjectName))
            resourceBuilder.AddAttributes([KeyValuePair.Create<string, object>("service.project.name", options.ProjectName)]);

        if (!string.IsNullOrEmpty(options.SsoClientId))
            resourceBuilder.AddAttributes([KeyValuePair.Create<string, object>("sso.client.id", options.SsoClientId)]);

        if (options.Device != null)
        {
            if (!string.IsNullOrEmpty(options.Device.Type))
                resourceBuilder.AddAttributes([KeyValuePair.Create<string, object>("device.type", options.Device.Type)]);
            if (!string.IsNullOrEmpty(options.Device.Platform))
                resourceBuilder.AddAttributes([KeyValuePair.Create<string, object>("device.platform", options.Device.Platform)]);
            if (!string.IsNullOrEmpty(options.Device.Version))
                resourceBuilder.AddAttributes([KeyValuePair.Create<string, object>("device.version", options.Device.Version)]);
            if (!string.IsNullOrEmpty(options.Device.Idiom))
                resourceBuilder.AddAttributes([KeyValuePair.Create<string, object>("device.Idiom", options.Device.Idiom)]);
            if (!string.IsNullOrEmpty(options.Device.Model))
                resourceBuilder.AddAttributes([KeyValuePair.Create<string, object>("device.model", options.Device.Model)]);
            if (!string.IsNullOrEmpty(options.Device.Manufacturer))
                resourceBuilder.AddAttributes([KeyValuePair.Create<string, object>("device.manufacturer", options.Device.Manufacturer)]);
            if (!string.IsNullOrEmpty(options.Device.Name))
                resourceBuilder.AddAttributes([KeyValuePair.Create<string, object>("device.name", options.Device.Name)]);
        }
        return resourceBuilder;
    }
}