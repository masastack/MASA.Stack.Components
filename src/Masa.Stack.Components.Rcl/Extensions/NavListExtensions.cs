using Masa.Stack.Components.Rcl.Models;

namespace Masa.Stack.Components.Rcl.Extensions;

internal static class NavListExtensions
{
    public static string GetDefaultRoute(this List<Nav> navs, string defaultRoute = "403")
    {
        var firstMenu = navs.FirstOrDefault();
        if (firstMenu != null)
        {
            if (string.IsNullOrEmpty(firstMenu.Url))
            {
                return firstMenu.Children.GetDefaultRoute();
            }

            return firstMenu.Url;
        }

        return defaultRoute;
    }
}
