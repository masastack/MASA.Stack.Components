using Masa.BuildingBlocks.BasicAbility.Auth.Model;

namespace Masa.Stack.Components;

public partial class GlobalNavigation : MasaComponentBase
{
    [Parameter]
    public RenderFragment<ActivatorProps> ActivatorContent { get; set; } = null!;

    [Parameter]
    public Func<string, Task>? OnFavoriteAdd { get; set; }

    [Parameter]
    public Func<string, Task>? OnFavoriteRemove { get; set; }

    private bool _visible;

    private List<FavoriteNav> FavoriteNavs { get; set; } = new();
    private List<(string name, string url)> RecentVisits { get; set; } = new();
    private List<Category> Categories { get; set; } = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Categories = await FetchCategories();

            var favorites = await FetchFavorites();

            FavoriteNavs = GetFavoriteNavs(favorites, Categories);

            RecentVisits = await GetRecentVisits();

            StateHasChanged();
        }
    }

    private async Task<List<Category>> FetchCategories()
    {
        var categories = await AuthClient.ProjectService.GetGlobalNavigations();
        var config = new TypeAdapterConfig();
        config.NewConfig<AppModel, App>()
            .Map(dest => dest.Code, src => src.Identity);
        config.NewConfig<ProjectModel, Category>()
            .Map(dest => dest.Code, src => src.Identity);
        return categories.Adapt<List<Category>>(config);
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

        FavoriteNav ConvertFavoriteNavs(List<CategoryAppNavModel> items, string code)
        {
            var result = new FavoriteNav();
            var favoriteItem = items.FirstOrDefault(f => f.Nav.Code == code);
            if (favoriteItem != null)
            {
                result = new FavoriteNav(favoriteItem.CategoryCode, favoriteItem.AppCode, favoriteItem.Nav);
            }
            else
            {
                result = ConvertFavoriteNavs(items.SelectMany(n => n.Nav.Children.Select(nav => new
                CategoryAppNavModel
                {
                    CategoryCode = n.CategoryCode,
                    AppCode = n.AppCode,
                    Nav = nav
                })).ToList(), code);
            }
            return result;
        }
    }

    private async Task<List<(string name, string url)>> GetRecentVisits()
    {
        var visitedList = await AuthClient.UserService.GetUserVisitedListAsync();
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
        var item = FavoriteNavs.FirstOrDefault(f => f.Id == favoriteNav.Id);
        if (item is not null)
        {
            if (OnFavoriteRemove is not null)
            {
                await OnFavoriteRemove.Invoke(item.Nav.Code);
            }

            FavoriteNavs.Remove(item);
        }
        else
        {
            if (OnFavoriteAdd is not null)
            {
                await OnFavoriteAdd.Invoke(nav.Code);
            }

            FavoriteNavs.Add(favoriteNav);
            nav.IsFavorite = true;
        }
    }

    private void VisitNav(Nav nav)
    {
        var item = RecentVisits.FirstOrDefault(r => r.name == nav.Name && r.url == nav.Url);

        if (item.name is not null)
        {
            RecentVisits.Remove(item);
        }

        RecentVisits.Insert(0, (nav.Name, nav.Url!));

        // TODO: add recentVisits
        _ = Task.Delay(500);
    }

    public void InvokeStateHasChanged() => this.StateHasChanged();
}
