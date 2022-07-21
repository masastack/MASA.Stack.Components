using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Masa.Stack.Components.Controllers;

[Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
public class AccountController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated == true)
        {
            // delete local authentication cookie
            await HttpContext.SignOutAsync();
        }

        foreach (var cookies in HttpContext.Request.Cookies)
        {
            HttpContext.Response.Cookies.Delete(cookies.Key);
        }

        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = "/"
            },
            "OpenIdConnect",
            "Cookies");
    }
}
