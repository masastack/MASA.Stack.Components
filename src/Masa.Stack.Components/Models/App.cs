namespace Masa.Stack.Components.Models;

public class App
{
    private List<Nav>? _navs = new();

    public string Code { get; set; }

    public string Name { get; set; }

    public List<Nav> Navs
    {
        get => _navs ?? new();
        set => _navs = value;
    }

    public App()
    {
        Code = "";
        Name = "";
    }

    public App(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public App(string code, string name, List<Nav> navs) : this(code, name)
    {
        Navs = navs;
    }

    internal bool Filter(DynamicTranslateProvider translateProvider, string? search) => string.IsNullOrEmpty(search) ? true : translateProvider.DT(Name).Contains(search, StringComparison.OrdinalIgnoreCase) || Navs.Any(nav => nav.Filter(translateProvider, search));

    public string TagId(string categoryCode, string? prefix) => $"{prefix}category-{categoryCode}-app-{Code}";

    public override bool Equals(object? obj)
    {
        return obj is App app && app.Code == Code && app.Navs.Except(Navs).Count() == 0;
    }

    public override int GetHashCode()
    {
        return 1;
    }
}