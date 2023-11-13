namespace Masa.Stack.Components.Server.Extensions.OpenIdConnect;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMasaOpenIdConnect(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddMasaOpenIdConnect(configuration.GetSection("$public.OIDC").Get<MasaOpenIdConnectOptions>());
    }

    public static IServiceCollection AddMasaOpenIdConnect(
        this IServiceCollection services,
        MasaOpenIdConnectOptions masaOpenIdConnectOptions)
    {
        services.AddSingleton(masaOpenIdConnectOptions);
        services.AddSingleton<LogoutSessionManager>();
        services.AddTransient<CookieEventHandler>();
        services.AddTransient<OidcEventHandler>();
        return services.AddMasaOpenIdConnect(masaOpenIdConnectOptions.Authority, masaOpenIdConnectOptions.ClientId,
                masaOpenIdConnectOptions.ClientSecret, masaOpenIdConnectOptions.Scopes.ToArray());
    }

    private static IServiceCollection AddMasaOpenIdConnect(
        this IServiceCollection services,
        string authority,
        string clinetId,
        string clientSecret,
        params string[] scopes)
    {
        services.AddHttpContextAccessor();

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromSeconds(5);
            options.EventsType = typeof(CookieEventHandler);
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = authority;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.SignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
            options.RequireHttpsMetadata = false;
            options.ClientId = clinetId;
            options.ClientSecret = clientSecret;
            options.ResponseType = OpenIdConnectResponseType.Code;
            foreach (var scope in scopes)
            {
                options.Scope.Add(scope);
            }

            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.UseTokenLifetime = true;

            options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(0);
            options.TokenValidationParameters.RequireExpirationTime = true;
            options.TokenValidationParameters.ValidateLifetime = true;

            options.NonceCookie.SameSite = SameSiteMode.Unspecified;
            options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;

            options.ClaimActions.MapAll();

            options.EventsType = typeof(OidcEventHandler);

            //ensure normal i use self signed certificate
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = delegate
                {
                    return true;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            // By default, all incoming requests will be authorized according to the default policy
            options.FallbackPolicy = options.DefaultPolicy;
        });

        return services;
    }
}
