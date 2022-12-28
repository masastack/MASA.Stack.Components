namespace Masa.Stack.Components.Extensions.OpenIdConnect;

public class CookieEventHandler : CookieAuthenticationEvents
{
    readonly ILogger<CookieEventHandler> _logger;

    public CookieEventHandler(ILogger<CookieEventHandler> logger)
    {
        _logger = logger;
    }

    public override Task SigningOut(CookieSigningOutContext context)
    {
        _logger.LogInformation("-----SigningOut-----");
        return base.SigningOut(context);
    }

    public override Task SignedIn(CookieSignedInContext context)
    {
        _logger.LogInformation("-----SignedIn-----");
        return base.SignedIn(context);
    }

    public override Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (context.Principal?.Identity?.IsAuthenticated == true)
        {
            var sub = context.Principal.FindFirst("sub")?.Value;
            var sid = context.Principal.FindFirst("sid")?.Value;

            //if (LogoutSessions.IsLoggedOut(sub, sid))
            //{
            //    context.RejectPrincipal();
            //    await context.HttpContext.SignOutAsync();
            //    // todo: if we have a refresh token, it should be revoked here.
            //}
        }
        return base.ValidatePrincipal(context);
    }
}
