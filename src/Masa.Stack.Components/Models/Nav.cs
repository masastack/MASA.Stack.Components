namespace Masa.Stack.Components.Models;

public class Nav
{
    private List<Nav>? _children;

    public string Code { get; set; }

    public string? Icon { get; set; }

    public string Name { get; set; }

    public int Level { get; set; }

    public string? ParentCode { get; set; }

    public string? ParentIcon { get; set; }

    public string? Target { get; set; }

    public string? Url { get; set; }

    public List<Nav> Children
    {
        get => _children ?? new();
        set => _children = value;
    }

    public bool IsFavorite { get; set; }

    public bool HasChildren => Children.Any();

    public bool IsActive(string url)
    {
        if (Url is null)
        {
            return false;
        }

        var tempUrl = Url;

        if (tempUrl.StartsWith("/"))
        {
            tempUrl = tempUrl[1..];
        }

        return string.Equals(tempUrl, url, StringComparison.OrdinalIgnoreCase);
    }

    public Nav()
    {
    }

    public Nav(string code, string name, string? url, int level)
    {
        Code = code;
        Name = name;
        Url = url;
        Level = level;
    }

    public Nav(string code, string name, string icon, string? url, int level) : this(code, name, url, level)
    {
        Icon = icon;
    }

    public Nav(string code, string name, string icon, string? url, int level, string parentCode) : this(code, name, icon, url, level)
    {
        ParentCode = parentCode;
    }

    public Nav(string code, string name, string icon, int level, List<Nav> children)
    {
        Code = code;
        Name = name;
        Icon = icon;
        Level = level;
        Children = children;
    }
}