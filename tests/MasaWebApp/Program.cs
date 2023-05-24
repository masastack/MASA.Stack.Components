// Ignore Spelling: app

using Masa.Contrib.StackSdks.Caller;
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
builder.AddMasaStackComponentsForServer(default);
builder.Services.AddMasaOpenIdConnect(new MasaOpenIdConnectOptions()
{
    Authority = "https://auth-sso-dev.masastack.com",
    ClientId = "masa.stack.web-development",
    Scopes = new List<string> { "offline_access" }
});

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