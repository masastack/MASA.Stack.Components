namespace Masa.Stack.Components.Extensions.OpenIdConnect;

public class OidcEventHandler : OpenIdConnectEvents
{
    readonly IDistributedCacheClient _distributedCacheClient;

    public OidcEventHandler(IDistributedCacheClient distributedCacheClient)
    {
        _distributedCacheClient = distributedCacheClient;
    }

    public override Task AccessDenied(AccessDeniedContext context)
    {
        context.HandleResponse();
        context.Response.Redirect("/");
        return base.AccessDenied(context);
    }

    public override Task RemoteFailure(RemoteFailureContext context)
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
        return base.RemoteFailure(context);
    }

    public override Task TokenValidated(TokenValidatedContext context)
    {
        var sub = context.Principal?.FindFirst("sub")?.Value ?? throw new InvalidOperationException("no sub claim");
        var refreshToken = context.TokenEndpointResponse?.RefreshToken;
        var accessToken = context.TokenEndpointResponse?.AccessToken;
        if (refreshToken != null || accessToken != null)
        {
            _distributedCacheClient.Set(sub, new TokenEndpoint
            {
                AccessToken = accessToken ?? "",
                RefreshToken = refreshToken ?? ""
            });
        }

        return base.TokenValidated(context);
    }

    public override Task RedirectToIdentityProviderForSignOut(RedirectContext context)
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
        return base.RedirectToIdentityProviderForSignOut(context);
    }
}
