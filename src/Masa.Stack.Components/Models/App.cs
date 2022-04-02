namespace Masa.Stack.Components.Models;

public partial class App
{
    public string Code { get; set; }

    public string Name { get; set; }

    public List<Nav> Navs { get; set; } = new();

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
    public string TagId(string categoryCode) => $"category-{categoryCode}-app-{Code}";
}