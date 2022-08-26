namespace Masa.Stack.Components.Controllers;

[Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Logout()
    {
        return SignOut("OpenIdConnect", "Cookies");
    }
}
