using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Masa.Stack.Components.Extensions.OpenIdConnect;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMasaOpenIdConnect(
        this WebAssemblyHostBuilder builder,
        IConfiguration configuration)
    {
        return builder.AddMasaOpenIdConnect(configuration.GetSection("$public.OIDC").Get<MasaOpenIdConnectOptions>());
    }

    public static IServiceCollection AddMasaOpenIdConnect(
        this WebAssemblyHostBuilder builder,
        MasaOpenIdConnectOptions masaOpenIdConnectOptions)
    {
        builder.Services.AddSingleton(masaOpenIdConnectOptions);
        builder.Services.AddSingleton<LogoutSessionManager>();
        //builder.Services.AddTransient<CookieEventHandler>();
        //services.AddTransient<OidcEventHandler>();
        return builder.AddMasaOpenIdConnect(masaOpenIdConnectOptions.Authority, masaOpenIdConnectOptions.ClientId,
                masaOpenIdConnectOptions.ClientSecret, masaOpenIdConnectOptions.Scopes.ToArray());
    }

    private static IServiceCollection AddMasaOpenIdConnect(
        this WebAssemblyHostBuilder builder,
        string authority,
        string clinetId,
        string clientSecret,
        params string[] scopes)
    {
        builder.Services.AddOidcAuthentication(options =>
        {
            builder.Configuration.Bind("oidc", options.ProviderOptions);
            options.ProviderOptions.AdditionalProviderParameters["environment"] = builder.HostEnvironment.Environment;
        });

        builder.Services.AddAuthorizationCore(options =>
        {
            options.FallbackPolicy = options.DefaultPolicy;
        });

        return builder.Services;
    }
}
