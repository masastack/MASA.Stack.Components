namespace Masa.Stack.Components;
using Masa.Stack.Components.GlobalNavigations;

public partial class GlobalNavigation : MasaComponentBase
{
    public const string MENU_URL_NAME = "url"; 
    [Parameter]
    public RenderFragment<ActivatorProps> ActivatorContent { get; set; } = null!;

    [Parameter]
    public Func<string, Task>? OnFavoriteAdd { get; set; }

    [Parameter]
    public Func<string, Task>? OnFavoriteRemove { get; set; }

    string _searchMenu = string.Empty;
    bool _visible;
    List<(string name, string url)> _recentVisits = new();
    List<Category> _categories { get; set; } = new();
    List<KeyValuePair<string, string>> _recommendApps = new();
    List<Menu> _favorites = new();
    Menu _menu;

    async Task IniDataAsync()
    {
        _searchMenu = string.Empty;
        (_menu, _categories) = await FetchCategories();
        _favorites = _menu.GetMenusByState(MenuState.Favorite);
        await GetRecommendApps();
        await GetRecentVisits();
    }

    private async Task GetRecommendApps()
    {
        //todo pm config
        var recommendAppIdentities = new List<string>() { MasaStackConfig.GetWebId(MasaStackConstant.PM), MasaStackConfig.GetWebId(MasaStackConstant.DCC), MasaStackConfig.GetWebId(MasaStackConstant.AUTH) };
        var projects = await PmClient.ProjectService.GetProjectAppsAsync(EnvironmentProvider.GetEnvironment());
        _recommendApps = projects.SelectMany(p => p.Apps).Where(a => recommendAppIdentities.Contains(a.Identity))
            .Select(a => new KeyValuePair<string, string>(a.Name, a.Url)).ToList();
    }

    public async Task VisibleChanged(bool visible)
    {
        if (visible)
        {
            await IniDataAsync();
        }
        _visible = visible;
    }

    private async Task<(Menu Menu, List<Category> Categories)> FetchCategories()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<AppModel, App>()
            .Map(dest => dest.Code, src => src.Identity);

        var menuMetadata = new MenuMetadata(MenuSituation.Favorite);
        var menu = new Menu("root", "root", MenuType.Root, MenuState.Normal, menuMetadata);
        try
        {
            var apps = (await AuthClient.ProjectService.GetGlobalNavigations()).SelectMany(p => p.Apps).ToList();
            var categories = apps.GroupBy(a => a.Tag).Select(ag => new Category(ag.Key, ag.Key, ag.Select(a => a.Adapt<App>(config)).Where(a => a.Navs.Any()).ToList())).ToList();
            var categorie = apps.GroupBy(a => a.Tag).ToList();
            var favorites = await FetchFavorites();

            foreach (var category in categorie)
            {
                var categoryMenu = new Menu(category.Key, category.Key, MenuType.Category, MenuState.Normal, menu.Metadata, parent: menu);
                foreach (var app in category)
                {
                    var appMenu = new Menu(app.Id.ToString(), app.Name, MenuType.App, MenuState.Normal, menu.Metadata, parent: categoryMenu);
                    foreach (var nav in app.Navs)
                    {
                        appMenu.Childrens.Add(ConvertForNav(nav, appMenu.Deep + 1, appMenu, favorites));
                    }
                    categoryMenu.Childrens.Add(appMenu); 
                }
                menu.Childrens.Add(categoryMenu);
            }
            
            return (menu, categories);
        }
        catch
        {

        }
        return (menu, new());
    }

    private Menu ConvertForNav(NavModel navModel, int deep, Menu parent, List<string> favorites)
    {
        var state = favorites.Any(favorite => favorite == navModel.Code) ? MenuState.Favorite : MenuState.Normal;
        var menu = new Menu(navModel.Code, navModel.Name, MenuType.Nav, state, parent.Metadata, parent: parent)
            .AddData(MENU_URL_NAME, navModel.Url);
        foreach (var childrenNav in navModel.Children)
        {
            menu.Childrens.Add(ConvertForNav(childrenNav, deep++, menu, favorites));
        }
        menu.Disabled = menu.Childrens.Count > 0;
        return menu;
    }

    private async Task<List<string>> FetchFavorites()
    {
        return (await AuthClient.PermissionService.GetFavoriteMenuListAsync())
            .Select(m => m.Value.ToString()).ToList();
    }
    
    private async Task GetRecentVisits()
    {
        var visitedList = await AuthClient.UserService.GetVisitedListAsync();
        _recentVisits = visitedList.Select(v => new ValueTuple<string, string>(v.Name, v.Url)).ToList();
    }

    private void NavigateTo(string? url)
    {
        if (url is null)
        {
            return;
        }

        NavigationManager.NavigateTo(url, forceLoad: true);
    }

    private Task FavoriteRemove(Menu nav)
    {
        // var favoriteNav = _favoriteNavs.FirstOrDefault(e => e.Nav == nav);
        // _favoriteNavs.Remove(favoriteNav);
        // await OnFavoriteRemove.Invoke(favoriteNav.Nav);
        return Task.CompletedTask;
    }

    private Task FavoriteChanged(List<CategoryAppNav> favoriteNavs)
    {
        return Task.CompletedTask;
        // favoriteNavs = favoriteNavs.Where(a => !a.NavModel!.HasChildren).ToList();
        // var removes = _favoriteNavs.Except(favoriteNavs);
        // foreach (var remove in removes)
        // {
        //     await OnFavoriteRemove.Invoke(remove.Nav);
        // }
        // var adds = favoriteNavs.Except(_favoriteNavs);
        // foreach (var add in adds)
        // {
        //     await OnFavoriteAdd.Invoke(add.Nav);
        // }
        // _favoriteNavs = favoriteNavs;
    }
}
