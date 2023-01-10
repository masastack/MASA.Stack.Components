namespace Masa.Stack.Components.Extensions.OpenIdConnect;

public class CookieEventHandler : CookieAuthenticationEvents
{
    readonly ILogger<CookieEventHandler> _logger;
    readonly LogoutSessionManager _logoutSessionManager;
    readonly MasaOpenIdConnectOptions _masaOpenIdConnectOptions;

    public CookieEventHandler(
        ILogger<CookieEventHandler> logger,
        LogoutSessionManager logoutSessionManager,
        MasaOpenIdConnectOptions masaOpenIdConnectOptions)
    {
        _logger = logger;
        _logoutSessionManager = logoutSessionManager;
        _masaOpenIdConnectOptions = masaOpenIdConnectOptions;
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

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (context.Principal?.Identity?.IsAuthenticated == true)
        {
            var sub = context.Principal.FindFirst("sub")?.Value;
            var sid = context.Principal.FindFirst("sid")?.Value;

            if (await _logoutSessionManager.IsLoggedOutAsync(sub, sid))
            {
                //    context.RejectPrincipal();
                //    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                //    //revoked refresh token
                //    var refreshToken = await context.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);
                //    var client = new HttpClient();
                //    var disco = await client.GetDiscoveryDocumentAsync(_masaOpenIdConnectOptions.Authority);
                //    var result = await client.RevokeTokenAsync(new TokenRevocationRequest
                //    {
                //        Address = disco.RevocationEndpoint,
                //        ClientId = _masaOpenIdConnectOptions.ClientId,
                //        TokenTypeHint = "refresh_token",
                //        Token = refreshToken
                //    });
            }
        }
    }
}
