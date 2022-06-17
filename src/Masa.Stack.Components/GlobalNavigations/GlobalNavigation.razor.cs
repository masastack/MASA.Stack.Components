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
        var categories = await authClient.ProjectService.GetGlobalNavigations();
        var config = new TypeAdapterConfig();
        config.NewConfig<AppModel, App>()
            .Map(dest => dest.Code, src => src.Identity);
        config.NewConfig<ProjectModel, Category>()
            .Map(dest => dest.Code, src => src.Identity);
        return categories.Adapt<List<Category>>(config);
    }

    private async Task<List<string>> FetchFavorites()
    {
        return (await authClient.PermissionService.GetCollectMenuListAsync())
            .Select(m => m.Value.ToString()).ToList();
    }

    private List<FavoriteNav> GetFavoriteNavs(List<string> favorites, List<Category> categories)
    {
        List<FavoriteNav> result = new();
        List<(string category, string app, string nav)> formattedFavorites = new();

        foreach (var favorite in favorites)
        {
            var res = favorite.Split("-");
            if (res.Length != 3)
            {
                continue;
            }

            formattedFavorites.Add((res[0], res[1], res[2]));
        }

        foreach (var category in categories)
        {
            var favorite = formattedFavorites.FirstOrDefault(f => f.category == category.Code);
            if (favorite.category is null)
            {
                continue;
            }

            var app = category.Apps.FirstOrDefault(a => a.Code == favorite.app);
            if (app is null)
            {
                continue;
            }

            var nav = app.Navs.SelectMany(n => n.Children ?? new()).FirstOrDefault(n => n.Code == favorite.nav);
            if (nav is null)
            {
                continue;
            }

            result.Add(new FavoriteNav(favorite.category, favorite.app, nav));
        }

        return result;
    }

    private async Task<List<(string name, string url)>> GetRecentVisits()
    {
        var visitedList = await authClient.UserService.GetUserVisitedListAsync();
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
