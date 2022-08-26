using Masa.BuildingBlocks.Configuration;
using Masa.Contrib.Configuration.ConfigurationApi.Dcc;
using Masa.Stack.Components;
using Masa.Utils.Security.Authentication.OpenIdConnect;
using MasaWebApp.Data;

var builder = WebApplication.CreateBuilder(args);
builder.AddMasaConfiguration(configurationBuilder =>
{
    configurationBuilder.UseDcc();
});
var publicConfiguration = builder.GetMasaConfiguration().ConfigurationApi.GetPublic();
builder.Services.AddMasaOpenIdConnect(publicConfiguration.GetSection("$public.OIDC:PmClient").Get<MasaOpenIdConnectOptions>());
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMasaStackComponentsForServer(default, "https://auth-service-develop.masastack.com/", builder.GetMasaConfiguration().Local["McServiceBaseAddress"]);

builder.Services.AddElasticsearchClient("auth", option => option.UseNodes("http://10.10.90.44:31920/").UseDefault())
                .AddAutoComplete(option => option.UseIndexName("user_index"));

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