namespace Masa.Stack.Components.Models;

public class NavModel
{
    public string Code { get; set; }

    public string? Icon { get; set; }

    public string Name { get; set; }

    public int Level { get; set; }

    public string ParentCode { get; set; }

    public string ParentIcon { get; set; }

    public string? Target { get; set; }

    public string? Url { get; set; }

    public List<NavModel>? Children { get; set; }

    public bool IsFavorite { get; set; }

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

    public NavModel()
    {
    }

    public NavModel(string code, string name, string icon, int level, List<NavModel> children)
    {
        Code = code;
        Name = name;
        Icon = icon;
        Level = level;
        Children = children;
    }

    public NavModel(string code, string name, string? url, int level)
    {
        Code = code;
        Name = name;
        Url = url;
        Level = level;
    }

    public NavModel(string code, string name, string icon, string? url, int level)
    {
        Code = code;
        Name = name;
        Icon = icon;
        Url = url;
        Level = level;
    }

    public NavModel(string code, string name, string icon, string? url, int level, string parentCode)
    {
        Code = code;
        Name = name;
        Icon = icon;
        Url = url;
        Level = level;
        ParentCode = parentCode;
    }
}