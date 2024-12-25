var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddSingleton(sp => builder.Configuration);
await builder.Services.AddMasaStackConfigAsync(builder.Configuration, builder.HostEnvironment.Environment);
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var masaStackConfig = builder.Services.GetMasaStackConfig();
builder.AddMasaOpenIdConnect(new MasaOpenIdConnectOptions
{
    Authority = masaStackConfig.GetSsoDomain(),
    ClientId = masaStackConfig.GetWebId(MasaStackProject.Auth),
    Scopes = new List<string> { "offline_access" }
});

builder.Services.AddMasaStackComponentsWithNormalApp(MasaStackProject.Auth, "http://localhost:4317", "1.0.0");
var host = builder.Build();
await host.Services.InitializeMasaStackApplicationAsync();
await host.RunAsync();