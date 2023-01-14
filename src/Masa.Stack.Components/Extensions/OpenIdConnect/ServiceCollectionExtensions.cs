namespace Masa.Stack.Components.Extensions.OpenIdConnect;

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
        return services.AddMasaOpenIdConnect(masaOpenIdConnectOptions.Authority, masaOpenIdConnectOptions.ClientId,
                masaOpenIdConnectOptions.ClientSecret, masaOpenIdConnectOptions.Scopes.ToArray());
    }

    public static IServiceCollection AddMasaOpenIdConnect(
        this IServiceCollection services,
        string authority,
        string clinetId,
        string clientSecret,
        params string[] scopes)
    {
        services.AddHttpContextAccessor();

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        services.AddTransient<CookieEventHandler>();
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

            options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(5.0);
            options.TokenValidationParameters.RequireExpirationTime = true;
            options.TokenValidationParameters.ValidateLifetime = true;

            options.NonceCookie.SameSite = SameSiteMode.Unspecified;
            options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;

            options.ClaimActions.MapUniqueJsonKey("account", "account");
            options.ClaimActions.MapUniqueJsonKey("roles", "roles");
            options.ClaimActions.MapUniqueJsonKey("environment", "environment");
            options.ClaimActions.MapUniqueJsonKey("current_team", "current_team");
            options.ClaimActions.MapUniqueJsonKey("phone_number", "phone_number");
            options.ClaimActions.MapUniqueJsonKey("staff_id", "staff_id");

            options.Events = new OpenIdConnectEvents
            {
                OnAccessDenied = context =>
                {
                    context.HandleResponse();
                    context.Response.Redirect("/");
                    return Task.CompletedTask;
                },
                OnRemoteFailure = context =>
                {
                    if (context.HttpContext.Request.Path.Value == "/signin-oidc")
                    {
                        context.SkipHandler();
                        context.Response.Redirect("/");
                    }
                    else
                    {
                        context.HandleResponse();
                    }
                    return Task.CompletedTask;
                },
                OnRedirectToIdentityProviderForSignOut = context =>
                {
                    if (context.Properties.Items.ContainsKey("env"))
                    {
                        context.ProtocolMessage.SetParameter("env",
                            context.Properties.Items["env"]);
                    }
                    if (context.Properties.Items.ContainsKey("RedirectToLogin"))
                    {
                        context.ProtocolMessage.SetParameter("RedirectToLogin",
                            context.Properties.Items["RedirectToLogin"]);
                    }
                    return Task.CompletedTask;
                },
                OnRedirectToIdentityProvider = context =>
                {
                    return Task.CompletedTask;
                },
                OnSignedOutCallbackRedirect = context =>
                {
                    return Task.CompletedTask;
                }
            };

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
