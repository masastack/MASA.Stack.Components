using BlazorComponent;
using Microsoft.JSInterop;

namespace Masa.Stack.Components;

public partial class GlobalNavigation : MasaComponentBase
{
    [Parameter]
    public RenderFragment<ActivatorProps> ActivatorContent { get; set; } = null!;

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

            RecentVisits = GetRecentVisits();

            StateHasChanged();
        }
    }

    private Task<List<Category>> FetchCategories()
    {
        var categories = new List<Category>()
        {
            new Category("basic-ability", "Basic Ability 基础能力", new List<App>()
            {
                new App("auth", "Auth", new List<Nav>()
                {
                    new Nav("user", "Users", "/users", 1, new List<Nav>()
                    {
                        new Nav("user-action1", "User Action 1"),
                        new Nav("user-action2", "User Action 2"),
                        new Nav("user-action3", "User Action 3"),
                    }),
                    new Nav("role-permission", "Role and Permission", "mdi-users", 1, new List<Nav>()
                    {
                        new Nav("role", "Roles", "/roles", 2),
                        new Nav("permission", "Permissions", "/permissions", 2, new List<Nav>()
                        {
                            new Nav("action1", "Action 1"),
                            new Nav("action2", "Action 2"),
                        }),
                    })
                }),
                new App("pm", "Project Management", new List<Nav>()
                {
                    new Nav("all", "All views", "/all-view", 1),
                    new Nav("groups", "Groups", "mdi-users", 1, new List<Nav>()
                    {
                        new Nav("group-1", "Group Name 1", "/group/1", 2, new List<Nav>()
                        {
                            new Nav("group-1-action1", "Action 1"),
                            new Nav("group-1-action2", "Action 2"),
                        }),
                        new Nav("group-2", "Group Name 2", "/group/2", 2),
                        new Nav("group-3", "Group Name 3", "/group/3", 2),
                        new Nav("group-4", "Group Name 4", "/group/4", 2),
                        new Nav("group-5", "Group Name 5", "/group/5", 2),
                    })
                }),
                new App("auth2", "Auth", new List<Nav>()
                {
                    new Nav("user", "Users", "/users", 1),
                    new Nav("role-permission", "Role and Permission", "mdi-users", 1, new List<Nav>()
                    {
                        new Nav("role", "Roles", "/roles", 2),
                        new Nav("permission", "Permissions", "/permissions", 2),
                    })
                }),
                new App("pm2", "Project Management", new List<Nav>()
                {
                    new Nav("all", "All views", "/all-view", 1),
                    new Nav("groups", "Groups", "mdi-users", 1, new List<Nav>()
                    {
                        new Nav("group-1", "Group Name 1", "/group/1", 2),
                        new Nav("group-2", "Group Name 2", "/group/2", 2),
                        new Nav("group-3", "Group Name 3", "/group/3", 2),
                        new Nav("group-4", "Group Name 4", "/group/4", 2),
                        new Nav("group-5", "Group Name 5", "/group/5", 2),
                        new Nav("group-6", "Group Name 6", "/group/6", 2),
                        new Nav("group-7", "Group Name 7", "/group/7", 2),
                    })
                }),
                new App("pm3", "Project Management", new List<Nav>()
                {
                    new Nav("all", "All views", "/all-view", 1),
                    new Nav("groups", "Groups", "mdi-users", 1, new List<Nav>()
                    {
                        new Nav("group-1", "Group Name 1", "/group/1", 2),
                        new Nav("group-2", "Group Name 2", "/group/2", 2),
                        new Nav("group-3", "Group Name 3", "/group/3", 2),
                    })
                }),
            }),
            new Category("basic-ability2", "Basic Ability 基础能力", new List<App>()
            {
                new App("auth", "Auth", new List<Nav>()
                {
                    new Nav("user", "Users", "/users", 1),
                    new Nav("role-permission", "Role and Permission", "mdi-users", 1, new List<Nav>()
                    {
                        new Nav("role", "Roles", "/roles", 2),
                        new Nav("permission", "Permissions", "/permissions", 2),
                        new Nav("bing", "Bing", "https://www.bing.com", 2),
                    })
                }),
                new App("pm111", "Project Management", new List<Nav>()
                {
                    new Nav("all", "All views", "/all-view", 1),
                    new Nav("groups", "Groups", "mdi-users", 1, new List<Nav>()
                    {
                        new Nav("group-1", "Group Name 1", "/group/1", 2),
                        new Nav("group-2", "Group Name 2", "/group/2", 2),
                        new Nav("group-3", "Group Name 3", "/group/3", 2),
                        new Nav("group-4", "Group Name 4", "/group/4", 2),
                        new Nav("group-5", "Group Name 5", "/group/5", 2),
                        new Nav("group-6", "Group Name 6", "/group/6", 2),
                        new Nav("group-7", "Group Name 7", "/group/7", 2),
                        new Nav("group-8", "Group Name 8", "/group/8", 2),
                        new Nav("group-9", "Group Name 9", "/group/9", 2),
                    })
                }),
            }),
        };

        return Task.FromResult(categories);
    }

    private Task<List<string>> FetchFavorites()
    {
        // TODO: fetch api or sdk
        return Task.FromResult(new List<string>());
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

    private List<(string name, string url)> GetRecentVisits()
    {
        // TODO: fetch recent visits
        return new List<(string name, string url)>()
        {
            ("User", "/users"),
            ("Permission", "/permissions"),
        };
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
            // TODO: remove favorite
            await Task.Delay(500);

            FavoriteNavs.Remove(item);

            nav.IsFavorite = false;
        }
        else
        {
            // TODO: add favorite
            await Task.Delay(500);

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
