using Masa.Stack.Components;
using MasaWebApp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMasaStackComponentsForServer("wwwroot/i18n", "https://auth-service-develop.masastack.com/", builder.Configuration["McServiceBaseAddress"]);
//builder.Services.AddMasaStackComponentsForServer("wwwroot/i18n", "http://localhost:18002/");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();