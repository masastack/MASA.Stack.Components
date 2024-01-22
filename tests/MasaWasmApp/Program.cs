using MasaWasmApp;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
//DccOptions dccOptions = builder.Configuration.GetSection("DccOptions").Get<DccOptions>();
//builder.Services.AddMasaConfiguration(configurationBuilder =>
//{
//    configurationBuilder.UseDcc();//使用Dcc 扩展Configuration能力，支持远程配置
//});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMasaBlazor();

builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("oidc", options.ProviderOptions);
});

builder.Services.AddAuthorizationCore(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

await builder.Build().RunAsync();
