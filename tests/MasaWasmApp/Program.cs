var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddSingleton(sp => builder.Configuration);
await builder.Services.AddMasaStackConfigAsync(builder.Configuration);
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var masaStackConfig = builder.Services.GetMasaStackConfig();
await builder.AddMasaOpenIdConnectAsync(new MasaOpenIdConnectOptions
{
    Authority = masaStackConfig.GetSsoDomain(),
    ClientId = masaStackConfig.GetWebId(MasaStackProject.Auth),
    Scopes = new List<string> { "openid", "profile", "offline_access" }
});

builder.Services.AddMasaStackComponent(MasaStackProject.Auth, "", microFrontend: false);
var host = builder.Build();
await host.Services.InitializeMasaStackApplicationAsync();
await host.RunAsync();