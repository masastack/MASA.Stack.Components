namespace Masa.Stack.Components.Models;

public class Nav : NavBase
{
    private List<Nav>? _children;

    public string? Icon { get; set; }

    public int Level { get; set; }

    public string? ParentCode { get; set; }

    public string? ParentIcon { get; set; }

    public string? Target { get; set; }

    public string? Url { get; set; }

    public List<Nav> Children
    {
        get
        {
            if (_children is null)
            {
                _children = new();
            }
            return _children;
        }
        set => _children = value;
    }
    //public List<Nav> Children { get; set; } = new();

    public bool IsAction { get; set; }

    public List<Nav> Actions => Children.Where(item => item.IsAction).ToList();

    public bool IsFavorite { get; set; }

    public bool IsDisabled { get; set; }

    public bool IsChecked { get; set; }

    public bool HasChildren => Children.Any() && !HasActions;

    public bool HasActions => Actions.Any();

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

    /// <summary>
    /// Initializes a nav-action.
    /// </summary>
    /// <param name="code"></param>
    /// <param name="name"></param>
    public Nav(string code, string name)
    {
        Code = code;
        Name = name;
        IsAction = true;
    }

    /// <summary>
    /// Initializes a nav-action.
    /// </summary>
    /// <param name="code"></param>
    /// <param name="name"></param>
    /// <param name="parentCode"></param>
    public Nav(string code, string name, string parentCode)
    {
        Code = code;
        Name = name;
        ParentCode = parentCode;
        IsAction = true;
    }

    public Nav(string code, string name, string? url, int level)
    {
        Code = code;
        Name = name;
        Url = url;
        Level = level;
    }

    public Nav(string code, string name, string url, int level, string parentCode)
    {
        Code = code;
        Name = name;
        Url = url;
        Level = level;
        ParentCode = parentCode;
    }

    public Nav(string code, string name, int level, List<Nav> children)
    {
        Code = code;
        Name = name;
        Level = level;
        Children = children;
    }

    public Nav(string code, string name, int level, string parentCode, List<Nav> children) : this(code, name, level, children)
    {
        ParentCode = parentCode;
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

    public override bool Equals(object? obj)
    {
        return obj is Nav nav && nav.Code == Code;
    }
}
