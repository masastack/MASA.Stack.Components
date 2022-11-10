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
    List<FavoriteNav> _favoriteNavs = new();
    List<(string name, string url)> _recentVisits = new();
    List<Category> _categories { get; set; } = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _categories = await FetchCategories();
            var favorites = await FetchFavorites();
            _favoriteNavs = GetFavoriteNavs(favorites, _categories);
            StateHasChanged();
        }
    }

    public async Task VisibleChanged(bool visible)
    {
        if (visible)
        {
            _recentVisits = await GetRecentVisits();
            _searchMenu = string.Empty;
            EnterSearch();
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
            var categories = apps.GroupBy(a => a.Tag).Select(ag => new Category
            {
                Code = ag.Key,
                Name = ag.Key,
                Apps = ag.Select(a => a.Adapt<App>(config)).Where(a => a.Navs.Any()).ToList()
            }).ToList();

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

    private List<FavoriteNav> GetFavoriteNavs(List<string> favorites, List<Category> categories)
    {
        List<FavoriteNav> result = new();

        var categoryAppNavs = categories.SelectMany(category =>
            category.Apps.SelectMany(app => app.Navs.Select(nav => new
            CategoryAppNavModel
            {
                CategoryCode = category.Code,
                AppCode = app.Code,
                Nav = nav
            }))).ToList();

        foreach (var favorite in favorites)
        {
            var favoriteItem = ConvertFavoriteNavs(categoryAppNavs, favorite);
            if (favoriteItem != null)
            {
                result.Add(new FavoriteNav(favoriteItem.CategoryCode, favoriteItem.AppCode, favoriteItem.Nav));
            }
        }

        result.ForEach(fn => fn.Nav.IsFavorite = true);

        return result;
        FavoriteNav? ConvertFavoriteNavs(List<CategoryAppNavModel> items, string code)
        {
            var favoriteItem = items.FirstOrDefault(f => f.Nav.Code == code);
            if (favoriteItem != null)
            {
                return new FavoriteNav(favoriteItem.CategoryCode, favoriteItem.AppCode, favoriteItem.Nav);
            }
            else
            {
                var children = items.SelectMany(n => n.Nav.Children.Select(nav => new CategoryAppNavModel
                {
                    CategoryCode = n.CategoryCode,
                    AppCode = n.AppCode,
                    Nav = nav
                })).ToList();
                if (children.Any())
                {
                    return ConvertFavoriteNavs(children, code);
                }
            }
            return null;
        }
    }

    private void EnterSearch()
    {
        FilterCategory(_searchMenu);
    }

    private void FilterCategory(string searchMenu)
    {
        foreach (var category in _categories)
        {
            foreach (var app in category.Apps)
            {
                Search(searchMenu, app.Navs);
            }
        }

        void Search(string searchMenu, List<Nav> items)
        {
            foreach (var item in items)
            {
                var displayName = DT(item.Name);
                item.Hiden = !displayName.Contains(searchMenu);
                if (item.Children.Any())
                {
                    Search(searchMenu, item.Children);
                }
            }
        }
    }

    private async Task<List<(string name, string url)>> GetRecentVisits()
    {
        var visitedList = await AuthClient.UserService.GetVisitedListAsync();
        return visitedList.Select(v => new ValueTuple<string, string>(v.Name, v.Url)).ToList();
    }

    private void NavigateTo(string? url)
    {
        if (url is null)
        {
            return;
        }

        NavigationManager.NavigateTo(url, forceLoad: true);
    }

    private async Task ToggleFavorite(string categoryCode, string appCode, Nav nav)
    {
        var favoriteNav = new FavoriteNav(categoryCode, appCode, nav);
        var item = _favoriteNavs.FirstOrDefault(f => f.Id == favoriteNav.Id);
        if (item is not null)
        {
            if (OnFavoriteRemove is not null)
            {
                await OnFavoriteRemove.Invoke(item.Nav.Code);
            }

            _favoriteNavs.Remove(item);
        }
        else
        {
            if (OnFavoriteAdd is not null)
            {
                await OnFavoriteAdd.Invoke(nav.Code);
            }

            _favoriteNavs.Add(favoriteNav);
            nav.IsFavorite = true;
        }
    }

    public void InvokeStateHasChanged() => this.StateHasChanged();
}
