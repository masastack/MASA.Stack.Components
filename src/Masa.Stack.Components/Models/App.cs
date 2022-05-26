namespace Masa.Stack.Components.Models;

public partial class App
{
    private List<Nav>? _navs = new();

    public string Code { get; set; }

    public string Name { get; set; }

    public List<Nav> Navs
    {
        get => _navs ?? new();
        set => _navs = value;
    }

    public int RoutableNavsCount => Navs.Any()
        ? Navs.SelectMany(n => n.Children).Concat(Navs).Count(n => !n.HasChildren)
        : 0;

    public App()
    {
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
}

public partial class App
{
    public string TagId(string categoryCode, string? prefix) => $"{prefix}category-{categoryCode}-app-{Code}";
}