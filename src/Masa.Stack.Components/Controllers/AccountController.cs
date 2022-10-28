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
    public IActionResult Logout(string? environment)
    {
        if (!string.IsNullOrEmpty(environment))
        {
            return SignOut(new AuthenticationProperties
            {
                Items = {
                    { IsolationConsts.ENVIRONMENT,environment }
                }
            }, "OpenIdConnect", "Cookies");
        }
        return SignOut("OpenIdConnect", "Cookies");
    }

    [HttpGet, HttpPut]
    public async Task UpsertClaimAsync(string key, string value)
    {
        await _authenticationStateManager.UpsertClaimAsync(key, value);
    }
}
