// Ignore Spelling: app

using Masa.Contrib.StackSdks.Caller;
using Masa.Contrib.StackSdks.Config;
using Masa.Stack.Components;
using Masa.Stack.Components.Extensions.OpenIdConnect;
using MasaWebApp.Data;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TokenProvider>();

await builder.Services.AddMasaStackConfigAsync(MasaStackProject.Auth, MasaStackApp.WEB);
var masaStackConfig = builder.Services.GetMasaStackConfig();
builder.Services.AddMasaOpenIdConnect(new MasaOpenIdConnectOptions
{
    Authority = masaStackConfig.GetSsoDomain(),
    ClientId = masaStackConfig.GetWebId(MasaStackProject.Auth),
    Scopes = new List<string> { "offline_access" }
});

await builder.Services.AddMasaStackComponentsAsync(new MasaStackProject(6, "test"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();