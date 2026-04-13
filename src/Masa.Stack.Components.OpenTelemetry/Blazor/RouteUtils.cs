namespace Masa.Stack.Components.OpenTelemetry.Blazor;

public sealed class RouteUtils
{
    private RouteUtils() { }

    internal static AppModuleDto? CurrentModule { get; private set; }

    internal static List<AppModuleDto> Modules { get; private set; } = [];

    internal static readonly Dictionary<Type, List<string>> _routes = [];

    internal static bool IsPage(Type type, NavigationManager navigation, string basePrefix, out string? routeTemplate)
    {
        return IsPage(type, navigation.Uri.Replace(navigation.BaseUri, basePrefix), out routeTemplate);
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

        // 使用 LINQ 简化对类型过滤和字典构建
        var routes = assembly
            .GetTypes()
            .Select(type => new { Type = type, Attributes = type.GetCustomAttributes<RouteAttribute>() })
            .Where(x => x.Attributes.Any())
            .ToDictionary(x => x.Type, x => x.Attributes.Select(a => a.Template).ToList());

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

    internal static void LoadRoutes()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        if (assemblies == null || assemblies.Length == 0)
            return;
        foreach (var assembly in assemblies)
        {
            var routes = GetRoutes(assembly);
            if (routes == null || routes.Count == 0) continue;
            // 使用 Where 简化过滤已有键的循环
            foreach (var item in routes.Where(kv => !_routes.ContainsKey(kv.Key)))
                _routes.Add(item.Key, item.Value);
        }
    }

    public static void SetModules(IEnumerable<AppModuleDto> modules)
    {
        if (modules == null || !modules.Any())
        {
            Modules = [];
            return;
        }
        Modules = [.. modules]!;
    }
}