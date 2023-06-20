namespace Masa.Stack.Components.Layouts;

public partial class SearchMenu
{
    [EditorRequired]
    [Parameter]
    public List<Nav> Navs { get; set; } = new();

    string _url = "";
    List<Menu> _menus = new();
    List<Nav> _navs = new();

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

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<List<Nav>>(nameof(Navs), out var navs) && !_navs.SequenceEqual(navs))
        {
            _menus = BuildMenus(GlobalConfig.Menus);
        }
        return base.SetParametersAsync(parameters);     
    }

    List<Menu> BuildMenus(List<Nav> navs, Menu? parent = null)
    {
        var menus = new List<Menu>();
        foreach (var nav in navs)
        {
            var menu = new Menu(nav.Code, nav.Name, nav.Icon, nav.Url ?? "/", parent);
            menus.Add(menu);
            if (nav.HasChildren) menus.AddRange(BuildMenus(nav.Children, menu));
        }
        return menus.Where(menu => !string.IsNullOrEmpty(menu.Url)).ToList();
    }

    string FullTitleI18n(string fullTitle)
    {
        return string.Join(' ', fullTitle.Split(' ').Select(title => DT(title)));
    }
}
