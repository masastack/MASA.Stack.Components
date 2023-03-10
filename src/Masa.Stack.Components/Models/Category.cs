namespace Masa.Stack.Components.Models;

public class Category
{
    public string Code { get; set; }

    public string Name { get; set; }

    public List<App> Apps { get; set; } = new();

    public Category(string code, string name, List<App> apps)
    {
        Code = code;
        Name = name;
        Apps = apps;
    }

    internal bool Filter(string? search) => string.IsNullOrEmpty(search) ? true : Name.Contains(search, StringComparison.OrdinalIgnoreCase) || Apps.Any(app => app.Filter(search));

    internal string TagId(string? prefix) => $"{prefix}category-{Code}";

    public override bool Equals(object? obj)
    {
        return obj is Category category && category.Code == Code;
    }

    public override int GetHashCode()
    {
        return 1;
    }
}