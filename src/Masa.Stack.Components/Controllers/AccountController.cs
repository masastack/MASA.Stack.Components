namespace Masa.Stack.Components.Controllers;

[Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
public class AccountController : Controller
{
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
}
