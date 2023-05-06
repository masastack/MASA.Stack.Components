﻿namespace Masa.Stack.Components;

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
    List<KeyValuePair<string, string>> _recommendApps = new();
    List<ExpansionMenu> _favorites = new();
    ExpansionMenu _menu;

    async Task IniDataAsync()
    {
        _searchMenu = string.Empty;
        _menu = await GenerateMenuAsync();
        _favorites = _menu.GetMenusByState(ExpansionMenuState.Favorite);
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

    private void SearchChanged(string? search)
    {
        Console.WriteLine(search);
        if (string.IsNullOrWhiteSpace(search))
        {
            return;
        }

        var s = _menu.Childrens.First();
        _menu.Childrens.Remove(s);
	
    }

    private void MenuItemClickAsync(ExpansionMenu menu)
    {
        var url = menu.GetData(MENU_URL_NAME);
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        NavigationManager.NavigateTo(url, true);
    }

    private async Task MenuItemOperClickAsync(ExpansionMenu menu)
    {
        if (menu.State == ExpansionMenuState.Normal)
        {
            await FavoriteRemoveAsync(menu);
        }
        else if (menu.State == ExpansionMenuState.Favorite)
        {
            await FavoriteAddAsync(menu);
        }
    }

    private async Task<ExpansionMenu> GenerateMenuAsync()
    {
        var menuMetadata = new ExpansionMenuMetadata(ExpansionMenuSituation.Favorite);
        var menu = new ExpansionMenu("root", "root", ExpansionMenuType.Root, ExpansionMenuState.Normal, menuMetadata);
        try
        {
            var apps = (await AuthClient.ProjectService.GetGlobalNavigations()).SelectMany(p => p.Apps).ToList();
            var categories = apps.GroupBy(a => a.Tag).ToList();
            var favorites = await FetchFavorites();

            foreach (var category in categories)
            {
                var categoryMenu = new ExpansionMenu(category.Key, category.Key, ExpansionMenuType.Category, ExpansionMenuState.Normal, menu.Metadata, parent: menu);
                foreach (var app in category.Where(a => a.Navs.Any()))
                {
                    var appMenu = new ExpansionMenu(app.Id.ToString(), app.Name, ExpansionMenuType.App, ExpansionMenuState.Normal, menu.Metadata, parent: categoryMenu);
                    foreach (var nav in app.Navs)
                    {
                        appMenu.Childrens.Add(ConvertForNav(nav, appMenu.Deep + 1, appMenu, favorites));
                    }
                    categoryMenu.Childrens.Add(appMenu); 
                }
                menu.Childrens.Add(categoryMenu);
            }
        }
        catch
        {
        }

        return menu;
    }

    private ExpansionMenu ConvertForNav(NavModel navModel, int deep, ExpansionMenu parent, List<string> favorites)
    {
        var state = favorites.Any(favorite => favorite == navModel.Code) ? ExpansionMenuState.Favorite : ExpansionMenuState.Normal;
        var menu = new ExpansionMenu(navModel.Code, navModel.Name, ExpansionMenuType.Nav, state, parent.Metadata, parent: parent)
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

    private async Task FavoriteRemoveAsync(ExpansionMenu nav)
    {
        var favoriteNav = _favorites.FirstOrDefault(e => e.Id == nav.Id);
        if (favoriteNav == null)
        {
            return;
        }

        _favorites.Remove(favoriteNav);
        if (OnFavoriteRemove != null)
        {
            await OnFavoriteRemove.Invoke(favoriteNav.Id);
        }
    }

    private async Task FavoriteAddAsync(ExpansionMenu nav)
    {
        if (_favorites.Any(e => e.Id == nav.Id))
        {
            return;
        }

        _favorites.Add(nav);
        if (OnFavoriteAdd != null)
        {
            await OnFavoriteAdd.Invoke(nav.Id);
        }
    }
}
