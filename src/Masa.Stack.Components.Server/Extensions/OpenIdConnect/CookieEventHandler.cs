namespace Masa.Stack.Components.Server.Extensions.OpenIdConnect;

public class CookieEventHandler : CookieAuthenticationEvents
{
    readonly ILogger<CookieEventHandler> _logger;
    readonly LogoutSessionManager _logoutSessionManager;
    readonly MasaOpenIdConnectOptions _masaOpenIdConnectOptions;
    readonly IDistributedCacheClient _distributedCacheClient;

    public CookieEventHandler(
        ILogger<CookieEventHandler> logger,
        LogoutSessionManager logoutSessionManager,
        MasaOpenIdConnectOptions masaOpenIdConnectOptions,
        IDistributedCacheClient distributedCacheClient)
    {
        _logger = logger;
        _logoutSessionManager = logoutSessionManager;
        _masaOpenIdConnectOptions = masaOpenIdConnectOptions;
        _distributedCacheClient = distributedCacheClient;
    }

    public override async Task SigningOut(CookieSigningOutContext context)
    {
        await base.SigningOut(context);
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (context.Principal?.Identity?.IsAuthenticated == true)
        {
            var sub = context.Principal.FindFirst("sub")?.Value;
            var sid = context.Principal.FindFirst("sid")?.Value;
            if (sub.IsNullOrEmpty() || sid.IsNullOrEmpty())
            {
                return;
            }
            if (_logoutSessionManager.IsLoggedOut(sub, sid))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                //revoked refresh token
                var tokenEndpoint = await _distributedCacheClient.GetAsync<TokenEndpoint>(sub);
                if (tokenEndpoint != null)
                {
                    var client = new HttpClient();
                    var disco = await client.GetDiscoveryDocumentAsync(_masaOpenIdConnectOptions.Authority);
                    var result = await client.RevokeTokenAsync(new TokenRevocationRequest
                    {
                        Address = disco.RevocationEndpoint,
                        ClientId = _masaOpenIdConnectOptions.ClientId,
                        TokenTypeHint = "refresh_token",
                        Token = tokenEndpoint.RefreshToken
                    });
                    if (result.IsError)
                    {
                        _logger.LogError(result.Exception, result.Error);
                    }
                }
            }
        }
    }
}
