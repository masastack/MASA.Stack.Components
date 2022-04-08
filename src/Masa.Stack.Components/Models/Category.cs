namespace Masa.Stack.Components.Models;

public partial class Category
{
    public string Code { get; set; }

    public string Name { get; set; }

    public List<App> Apps { get; set; } = new();

    public Category()
    {
    }

    public Category(string code, string name, List<App> apps)
    {
        Code = code;
        Name = name;
        Apps = apps;
    }
}

public partial class Category
{
    internal string TagId() => $"category-{Code}";

    internal string TagStyle { get; set; }

    internal List<StringNumber> BindValues { get; set; }
}