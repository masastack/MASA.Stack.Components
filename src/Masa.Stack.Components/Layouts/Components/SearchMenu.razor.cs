namespace Masa.Stack.Components.Layouts;

public partial class SearchMenu
{
    List<Menu>? _menus;
    string _url = "";

    List<Menu> Menus
    {
        get
        {
            if (_menus?.Any() is not true) _menus = BuildMenus(GlobalConfig.Menus);
            return _menus ?? new();
        }
    }

    public string Url
    {
        get => _url;
        set
        {
            if (_url != value)
            {
                _url = value;
                if (string.IsNullOrEmpty(_url) is false) NavigationManager.NavigateTo(_url);
            }
        }
    }

    List<Menu> BuildMenus(List<Nav> navs, Menu? parent = null)
    {
        var menus = new List<Menu>();
        foreach (var nav in navs)
        {
            var menu = new Menu(nav.Code, nav.Name, nav.Icon, nav.Url, parent);
            menus.Add(menu);
            if (nav.HasChildren) menus.AddRange(BuildMenus(nav.Children, menu));
        }
        return menus.Where(menu => string.IsNullOrEmpty(menu.Url) is false).ToList();
    }

    string FullTitleI18n(string fullTitle)
    {
        return string.Join(' ', fullTitle.Split(' ').Select(title => T(title)));
    }
}
