using Masa.BuildingBlocks.StackSdks.Config;
using Masa.Stack.Components.OpenTelemetry.Blazor;
using Masa.Stack.Components.OpenTelemetry.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddSingleton(sp => builder.Configuration);
await builder.Services.AddMasaStackConfigAsync(builder.Configuration);
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var masaStackConfig = builder.Services.GetMasaStackConfig();

builder.Services.AddMasaBlazorWasmObservable(builder.Logging, new MasaBlazorWasmObservableOptions
{
    ServiceName = MasaStackConfigConstant.MASA_STACK,
    ProjectName = MasaStackProject.Auth.Name,
    ServiceNameSpace = builder.HostEnvironment.Environment,
    ServiceVersion = masaStackConfig.Version,
    SsoClientId = MasaStackConfigConstant.MASA_STACK
}, otelUrl: masaStackConfig.OtlpUrl);

RouteUtils.LoadRoutes([typeof(Program).Assembly, typeof(MasaComponentBase).Assembly]);
await builder.AddMasaOpenIdConnectAsync(new MasaOpenIdConnectOptions
{
    Authority = masaStackConfig.GetSsoDomain(),
    ClientId = MasaStackConfigConstant.MASA_STACK,
    Scopes = new List<string> { "openid", "profile", "offline_access" }
});

builder.Services.AddMasaStackComponent(MasaStackProject.Auth, "", microFrontend: false);
var host = builder.Build();
await host.Services.InitializeMasaStackApplicationAsync();
await host.RunAsync();