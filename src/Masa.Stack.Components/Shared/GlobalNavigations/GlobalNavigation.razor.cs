namespace Masa.Stack.Components;

public partial class GlobalNavigation : MasaComponentBase
{
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
    List<CategoryAppNav> _favoriteNavs = new();

    async Task IniDataAsync()
    {
        _searchMenu = string.Empty;
        _categories = await FetchCategories();
        await GetFavoriteNavs(_categories);
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

    private async Task<List<Category>> FetchCategories()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<AppModel, App>()
            .Map(dest => dest.Code, src => src.Identity);

        try
        {
            var apps = (await AuthClient.ProjectService.GetGlobalNavigations()).SelectMany(p => p.Apps).ToList();
            var categories = apps.GroupBy(a => a.Tag).Select(ag => new Category(ag.Key, ag.Key, ag.Select(a => a.Adapt<App>(config)).Where(a => a.Navs.Any()).ToList())).ToList();

            return categories;
        }
        catch
        {

        }

        return new();
    }

    private async Task<List<string>> FetchFavorites()
    {
        return (await AuthClient.PermissionService.GetFavoriteMenuListAsync())
            .Select(m => m.Value.ToString()).ToList();
    }

    private async Task GetFavoriteNavs(List<Category> categories)
    {
        _favoriteNavs.Clear();

        var categoryAppNavs = categories.SelectMany(category =>
            category.Apps.SelectMany(app => app.Navs.Select(nav => new
            CategoryAppNavModel(category.Code, app.Code, nav)))).ToList();

        var favorites = await FetchFavorites();

        foreach (var favorite in favorites)
        {
            var favoriteItem = ConvertFavoriteNavs(categoryAppNavs, favorite);
            if (favoriteItem != null)
            {
                _favoriteNavs.Add(favoriteItem);
            }
        }

        CategoryAppNav? ConvertFavoriteNavs(List<CategoryAppNavModel> items, string code)
        {
            var favoriteItem = items.FirstOrDefault(f => f.Nav.Code == code);
            if (favoriteItem != null)
            {
                return new CategoryAppNav(favoriteItem.CategoryCode, favoriteItem.AppCode, favoriteItem.Nav.Code, default, favoriteItem.Nav);
            }
            else
            {
                var children = items.SelectMany(n => n.Nav.Children.Select(nav => new CategoryAppNavModel(n.CategoryCode, n.AppCode, nav))).ToList();
                if (children.Any())
                {
                    return ConvertFavoriteNavs(children, code);
                }
            }
            return null;
        }
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

    private async Task FavoriteChanged(List<CategoryAppNav> favoriteNavs)
    {
        var removes = _favoriteNavs.Except(favoriteNavs);
        foreach (var remove in removes)
        {
            await OnFavoriteRemove.Invoke(remove.Nav);
        }
        var adds = favoriteNavs.Except(_favoriteNavs);
        foreach (var add in adds)
        {
            await OnFavoriteAdd.Invoke(add.Nav);
        }
        _favoriteNavs = favoriteNavs;
    }
}
