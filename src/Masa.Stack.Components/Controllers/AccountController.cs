namespace Masa.Stack.Components.Controllers;

[Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
public class AccountController : Controller
{
    readonly AuthenticationStateManager _authenticationStateManager;
    readonly LogoutSessionManager _logoutSessionManager;
    readonly ILogger<AccountController> _logger;
    readonly MasaOpenIdConnectOptions _masaOpenIdConnectOptions;

    public AccountController(
        AuthenticationStateManager authenticationStateManager,
        LogoutSessionManager logoutSessionManager,
        ILogger<AccountController> logger,
        MasaOpenIdConnectOptions masaOpenIdConnectOptions)
    {
        _authenticationStateManager = authenticationStateManager;
        _logoutSessionManager = logoutSessionManager;
        _logger = logger;
        _masaOpenIdConnectOptions = masaOpenIdConnectOptions;
    }

    [HttpGet]
    public IActionResult Logout(string? environment, bool redirectToLogin = false)
    {
        var authenticationProperties = new AuthenticationProperties
        {
            Items = {
                { PropertyConsts.REDIRECT_TO_LOGIN,redirectToLogin.ToString() }
            }
        };
        if (!string.IsNullOrEmpty(environment))
        {
            authenticationProperties.Items.TryAdd(IsolationConsts.ENVIRONMENT, environment);
        }
        return SignOut(authenticationProperties, "OpenIdConnect", "Cookies");
    }

    [HttpGet, HttpPut]
    public async Task UpsertClaimAsync(string key, string value)
    {
        await _authenticationStateManager.UpsertClaimAsync(key, value);
    }

    [HttpGet]
    public async Task<IActionResult> FrontChannelLogout(string sid)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentSid = User.FindFirst("sid")?.Value ?? "";
            if (string.Equals(currentSid, sid, StringComparison.Ordinal))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            }
        }
        return NoContent();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> BackChannelLogout(string logout_token)
    {
        Response.Headers.Add("Cache-Control", "no-cache, no-store");
        Response.Headers.Add("Pragma", "no-cache");

        try
        {
            var user = await ValidateLogoutToken(logout_token);
            var sub = user.FindFirst("sub")?.Value;
            var sid = user.FindFirst("sid")?.Value;

            if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(sid))
            {
                _logger.LogWarning("sub or sid is empty");
                return BadRequest();
            }
            //_logoutSessionManager.Add(sub, sid);

            return Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "BackChannelLogout");
        }

        return BadRequest();
    }

    private async Task<ClaimsPrincipal> ValidateLogoutToken(string logoutToken)
    {
        var claims = await ValidateJwt(logoutToken);

        if (claims.FindFirst("sub") == null && claims.FindFirst("sid") == null)
            throw new Exception("Invalid logout token");

        var nonce = claims.FindFirstValue("nonce");
        if (!string.IsNullOrWhiteSpace(nonce))
            throw new Exception("Invalid logout token");

        var eventsJson = claims.FindFirst("events")?.Value;
        if (string.IsNullOrWhiteSpace(eventsJson))
            throw new Exception("Invalid logout token");

        var events = JsonObject.Parse(eventsJson);
        //var logoutEvent = events.TryGetValue("http://schemas.openid.net/event/backchannel-logout");
        if (events is null)
            throw new Exception("Invalid logout token");

        return claims;
    }

    private async Task<ClaimsPrincipal> ValidateJwt(string jwt)
    {
        var client = new HttpClient();
        var disco = await client.GetDiscoveryDocumentAsync(_masaOpenIdConnectOptions.Authority);

        var keys = new List<SecurityKey>();
        foreach (var webKey in disco.KeySet.Keys)
        {
            var key = new JsonWebKey()
            {
                Kty = webKey.Kty,
                Alg = webKey.Alg,
                Kid = webKey.Kid,
                X = webKey.X,
                Y = webKey.Y,
                Crv = webKey.Crv,
                E = webKey.E,
                N = webKey.N,
            };
            keys.Add(key);
        }

        var parameters = new TokenValidationParameters
        {
            ValidIssuer = disco.Issuer,
            ValidAudience = _masaOpenIdConnectOptions.ClientId,
            IssuerSigningKeys = keys,
        };

        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear();

        var user = handler.ValidateToken(jwt, parameters, out var _);
        return user;
    }
}
