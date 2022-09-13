using Masa.BuildingBlocks.Configuration;
using Masa.BuildingBlocks.StackSdks.Auth.Contracts.Provider;
using Masa.Contrib.Configuration.ConfigurationApi.Dcc;
using Masa.Stack.Components;
using Masa.Utils.Security.Authentication.OpenIdConnect;
using MasaWebApp.Data;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TokenProvider>();
builder.AddMasaStackComponentsForServer(default);
var publicConfiguration = builder.GetMasaConfiguration().ConfigurationApi.GetPublic();
builder.Services.AddMasaOpenIdConnect(publicConfiguration.GetSection("$public.OIDC:PMClient").Get<MasaOpenIdConnectOptions>());

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