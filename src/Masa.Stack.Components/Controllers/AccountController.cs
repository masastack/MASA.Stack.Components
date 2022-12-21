using Masa.Stack.Components.Infrastructure.Identity;

namespace Masa.Stack.Components.Controllers;

[Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
public class AccountController : Controller
{
    readonly AuthenticationStateManager _authenticationStateManager;

    public AccountController(AuthenticationStateManager authenticationStateManager)
    {
        _authenticationStateManager = authenticationStateManager;
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
}
