namespace Masa.Stack.Components.Extensions;

internal static class NavListExtensions
{
    public static string GetDefaultRoute(this List<Nav> navs, string defaultRoute = "403", string projectPrefix = "/")
    {
        var firstMenu = navs.FirstOrDefault();
        if (firstMenu != null)
        {
            if (string.IsNullOrEmpty(firstMenu.Url))
            {
                return GetDefaultRoute(firstMenu.Children);
            }

            var routeUrl = firstMenu.Url;
            if (routeUrl.StartsWith($"/{projectPrefix}/"))
            {
                routeUrl = routeUrl.Replace($"/{projectPrefix}/", "/");
            }

            return routeUrl;
        }
        return defaultRoute;
    }

    public static void AddPrefixToUrls(this List<Nav> navList, string projectPrefix)
    {
        if (navList == null || string.IsNullOrWhiteSpace(projectPrefix)) return;

        foreach (var nav in navList)
        {
            if (!string.IsNullOrWhiteSpace(nav.Url))
            {
                nav.Url = $"/{projectPrefix.TrimEnd('/')}/{nav.Url.TrimStart('/')}";
            }

            if (nav.Children != null && nav.Children.Any())
            {
                AddPrefixToUrls(nav.Children, projectPrefix);
            }
        }
    }
}
