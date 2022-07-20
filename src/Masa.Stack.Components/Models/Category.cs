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

    public override bool Equals(object? obj)
    {
        return obj is Category category && category.Code == Code;
    }
}

public partial class Category
{
    internal string TagId(string? prefix) => $"{prefix}category-{Code}";

    internal string TagStyle { get; set; }

    internal List<StringNumber>? BindValues { get; set; }
}