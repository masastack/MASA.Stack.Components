namespace Masa.Stack.Components.OpenTelemetry.Blazor;

public sealed class RouteUtils
{
    private RouteUtils() { }

    internal static AppModuleDto? CurrentModule { get; private set; }

    internal static List<AppModuleDto> Modules { get; private set; } = [];

    internal static Dictionary<Type, List<string>> _routes = GetRoutes();

    internal static bool IsPage(Type type, NavigationManager navigation, out string? routeTemplate)
    {
        return IsPage(type, navigation.Uri.Replace(navigation.BaseUri, "/"), out routeTemplate);
    }

    internal static bool IsPage(Type type, string url, out string? routeTemplate)
    {
        routeTemplate = default;
        if (type == null || !_routes.TryGetValue(type, out _) || string.IsNullOrEmpty(url))
            return default;

        var routes = _routes[type];
        routeTemplate = GetSelfTemplate(url, routes);
        if (string.IsNullOrEmpty(routeTemplate))
        {
            //需要记录下来
        }
        return !string.IsNullOrEmpty(routeTemplate);
    }

    static Dictionary<Type, List<string>> GetRoutes(Assembly? assembly = default)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        var routes = new Dictionary<Type, List<string>>();
        foreach (var type in assembly.GetTypes())
        {
            var attributes = type.GetCustomAttributes<RouteAttribute>();
            if (attributes.Any())
            {
                routes.Add(type, attributes.Select(a => a.Template).ToList());
            }
        }

        return routes;
    }

    internal static string? GetSelfTemplate(string url, List<string> routers)
    {
        if (routers.Count == 1)
            return routers[0];

        var arrayA = url.Split('?')[0].Split('/');
        var matchRoutes = new Dictionary<string, int>();
        foreach (var route in routers)
        {
            var arrayB = route.Split('/');
            (bool isMatch, int length) = CheckUrlRouter(arrayB, arrayA);
            if (isMatch)
                matchRoutes.Add(route, length);
        }
        if (matchRoutes.Count > 0)
            return matchRoutes.OrderBy(d => d.Value).First().Key;

        return default;
    }

    private static (bool, int) CheckUrlRouter(string[] routes, string[] b)
    {
        int routerIndex = 0, emptyCount = 0;
        for (; routes.Length - routerIndex > 0 && b.Length - routerIndex + emptyCount > 0; routerIndex++)
        {
            if (routes[routerIndex].StartsWith('{') && routes[routerIndex].EndsWith('}'))
            {
                if (routes[routerIndex].EndsWith("?}"))
                {
                    emptyCount++;
                    //if (routerIndex > 0)
                    //    return (true, routerIndex);
                    //else
                    //    return (false, 0);
                }
                continue;
            }
            if (!routes[routerIndex].Equals(b[routerIndex], StringComparison.CurrentCultureIgnoreCase))
                return (false, 0);
        }
        bool isMatched = routes.Length - routerIndex == 0;
        return (isMatched, isMatched ? routerIndex : 0);
    }

    internal static void ChangeModule(AppModuleDto? module)
    {
        CurrentModule = module;
    }

    internal static AppModuleDto? GetModule(string url)
    {
        if (url == "/")
            return default;
        foreach (var module in Modules)
        {
            if (!string.IsNullOrEmpty(module.Router) && module.Router != "/" && url.StartsWith(module.Router, StringComparison.OrdinalIgnoreCase))
                return module;
        }

        return default;
    }

    public static void LoadRoutes([NotNull] List<Assembly> assemblies)
    {
        if (assemblies == null || assemblies.Count == 0)
            return;
        foreach (var assembly in assemblies)
        {
            var routes = GetRoutes(assembly);
            foreach (var item in routes)
            {
                if (!_routes.ContainsKey(item.Key))
                    _routes.Add(item.Key, item.Value);
            }
        }
    }
}