using BlazorComponent;
using Microsoft.JSInterop;

namespace Masa.Stack.Components.Layouts;

public partial class GlobalNavigation
{
    [Inject]
    public IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [Parameter]
    public RenderFragment<ActivatorProps> ActivatorContent { get; set; } = null!;

    private bool _visible;

    private List<FavoriteNav> FavoriteNavs { get; set; } = new();
    private List<(string name, string url)> RecentVisits { get; set; } = new();
    private List<Topic> Topics { get; set; } = new();
    private Dictionary<string, List<StringNumber>> TopicCodes { get; set; } = new();

    private bool _refreshWaterFull;
    private bool _isFirstVisible = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Topics = await FetchTopics();

            Topics.ForEach(topic => { TopicCodes.Add(topic.Code, topic.Apps.Select(a => (StringNumber)a.Code).ToList()); });

            var favorites = await FetchFavorites();

            FavoriteNavs = GetFavoriteNavs(favorites, Topics);

            RecentVisits = GetRecentVisits();

            StateHasChanged();
        }

        if ((_visible && _isFirstVisible) || _refreshWaterFull)
        {
            _isFirstVisible = false;

            if (Topics.Any())
            {
                _refreshWaterFull = false;

                // TODO: How to wait for elements to be rendered
                await Task.Delay(256);

                foreach (var topic in Topics)
                {
                    await ResizeNav(topic);
                }

                StateHasChanged();

            }
        }
    }

    private async Task ResizeNav(Topic topic)
    {
        var height = await JsRuntime.InvokeAsync<double>(
            "MasaStackComponents.waterFull",
            $"#{topic.TagId()} .apps",
            ".app");

        topic.TagStyle = $"position:relative; height:{height}px;";
    }

    private void TopicValuesChanged(List<StringNumber> values, Topic topic)
    {
        topic.BindValues = values;
        _refreshWaterFull = true;
    }

    private Task<List<Topic>> FetchTopics()
    {
        var topics = new List<Topic>()
        {
            new Topic("basic-ability", "Basic Ability 基础能力", new List<App>()
            {
                new App("auth", "Auth", new List<NavModel>()
                {
                    new NavModel("user", "Users", "/users", 1),
                    new NavModel("role-permission", "Role and Permission", "mdi-users", 1, new List<NavModel>()
                    {
                        new NavModel("role", "Roles", "/roles", 2),
                        new NavModel("permission", "Permissions", "/permissions", 2),
                    })
                }),
                new App("pm", "Project Management 项目管理", new List<NavModel>()
                {
                    new NavModel("all", "All views", "/all-view", 1),
                    new NavModel("groups", "Groups", "mdi-users", 1, new List<NavModel>()
                    {
                        new NavModel("group-1", "Group Name 1", "/group/1", 2),
                        new NavModel("group-2", "Group Name 2", "/group/2", 2),
                        new NavModel("group-3", "Group Name 3", "/group/3", 2),
                        new NavModel("group-4", "Group Name 4", "/group/4", 2),
                        new NavModel("group-5", "Group Name 5", "/group/5", 2),
                    })
                }),
                new App("auth2", "Auth", new List<NavModel>()
                {
                    new NavModel("user", "Users", "/users", 1),
                    new NavModel("role-permission", "Role and Permission", "mdi-users", 1, new List<NavModel>()
                    {
                        new NavModel("role", "Roles", "/roles", 2),
                        new NavModel("permission", "Permissions", "/permissions", 2),
                    })
                }),
                new App("pm2", "Project Management 项目管理", new List<NavModel>()
                {
                    new NavModel("all", "All views", "/all-view", 1),
                    new NavModel("groups", "Groups", "mdi-users", 1, new List<NavModel>()
                    {
                        new NavModel("group-1", "Group Name 1", "/group/1", 2),
                        new NavModel("group-2", "Group Name 2", "/group/2", 2),
                        new NavModel("group-3", "Group Name 3", "/group/3", 2),
                        new NavModel("group-4", "Group Name 4", "/group/4", 2),
                        new NavModel("group-5", "Group Name 5", "/group/5", 2),
                        new NavModel("group-6", "Group Name 6", "/group/6", 2),
                        new NavModel("group-7", "Group Name 7", "/group/7", 2),
                    })
                }),
                new App("pm3", "Project Management 项目管理", new List<NavModel>()
                {
                    new NavModel("all", "All views", "/all-view", 1),
                    new NavModel("groups", "Groups", "mdi-users", 1, new List<NavModel>()
                    {
                        new NavModel("group-1", "Group Name 1", "/group/1", 2),
                        new NavModel("group-2", "Group Name 2", "/group/2", 2),
                        new NavModel("group-3", "Group Name 3", "/group/3", 2),
                    })
                }),
            }),
            new Topic("basic-ability2", "Basic Ability 基础能力", new List<App>()
            {
                new App("auth", "Auth", new List<NavModel>()
                {
                    new NavModel("user", "Users", "/users", 1),
                    new NavModel("role-permission", "Role and Permission", "mdi-users", 1, new List<NavModel>()
                    {
                        new NavModel("role", "Roles", "/roles", 2),
                        new NavModel("permission", "Permissions", "/permissions", 2),
                        new NavModel("bing", "Bing", "https://www.bing.com", 2),
                    })
                }),
                new App("pm", "Project Management 项目管理", new List<NavModel>()
                {
                    new NavModel("all", "All views", "/all-view", 1),
                    new NavModel("groups", "Groups", "mdi-users", 1, new List<NavModel>()
                    {
                        new NavModel("group-1", "Group Name 1", "/group/1", 2),
                        new NavModel("group-2", "Group Name 2", "/group/2", 2),
                        new NavModel("group-3", "Group Name 3", "/group/3", 2),
                        new NavModel("group-4", "Group Name 4", "/group/4", 2),
                        new NavModel("group-5", "Group Name 5", "/group/5", 2),
                        new NavModel("group-6", "Group Name 6", "/group/6", 2),
                        new NavModel("group-7", "Group Name 7", "/group/7", 2),
                        new NavModel("group-8", "Group Name 8", "/group/8", 2),
                        new NavModel("group-9", "Group Name 9", "/group/9", 2),
                    })
                }),
            }),
        };

        return Task.FromResult(topics);
    }

    private Task<List<string>> FetchFavorites()
    {
        // TODO: fetch api or sdk
        return Task.FromResult(new List<string>());
    }

    private List<FavoriteNav> GetFavoriteNavs(List<string> favorites, List<Topic> topics)
    {
        List<FavoriteNav> result = new();
        List<(string topic, string app, string nav)> formattedFavorites = new();

        foreach (var favorite in favorites)
        {
            var res = favorite.Split("-");
            if (res.Length != 3)
            {
                continue;
            }

            formattedFavorites.Add((res[0], res[1], res[2]));
        }

        foreach (var topic in topics)
        {
            var favorite = formattedFavorites.FirstOrDefault(f => f.topic == topic.Code);
            if (favorite.topic is null)
            {
                continue;
            }

            var app = topic.Apps.FirstOrDefault(a => a.Code == favorite.app);
            if (app is null)
            {
                continue;
            }

            var nav = app.Navs.SelectMany(n => n.Children).FirstOrDefault(n => n.Code == favorite.nav);
            if (nav is null)
            {
                continue;
            }

            result.Add(new FavoriteNav(favorite.topic, favorite.app, nav));
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

    private void NavigateTo(string url)
    {
        NavigationManager.NavigateTo(url, forceLoad: true);
    }

    private async Task ToggleFavorite(string topic, string app, NavModel nav)
    {
        var favoriteNav = new FavoriteNav(topic, app, nav);
        var item = FavoriteNavs.FirstOrDefault(f => f.Id == favoriteNav.Id);
        if (item is not null)
        {
            // TODO: remove favorite
            await Task.Delay(1000);

            FavoriteNavs.Remove(item);

            nav.IsFavorite = false;
        }
        else
        {
            // TODO: add favorite
            await Task.Delay(1000);

            FavoriteNavs.Add(favoriteNav);

            nav.IsFavorite = true;
        }
    }

    private void VisitNav(NavModel nav)
    {
        var item = RecentVisits.FirstOrDefault(r => r.name == nav.Name && r.url == nav.Url);

        if (item.name is not null)
        {
            RecentVisits.Remove(item);
        }

        RecentVisits.Insert(0, (nav.Name, nav.Url!));

        // TODO: add recentVisits
        _ = Task.Delay(1000);
    }

    private async Task ScrollTo(string tagId, string insideSelector)
    {
        await JsRuntime.InvokeVoidAsync("MasaStackComponents.scrollTo", $"#{tagId}", insideSelector);
    }
}

public class FavoriteNav
{
    public string Topic { get; set; }

    public string App { get; set; }

    public NavModel Nav { get; set; }

    public string Id => $"{Topic}-{App}-{Nav.Code}";

    public FavoriteNav()
    {
    }

    public FavoriteNav(string topic, string app, NavModel nav)
    {
        Topic = topic;
        App = app;
        Nav = nav;
    }
}

public partial class Topic
{
    public string Code { get; set; }

    public string Name { get; set; }

    public List<App> Apps { get; set; } = new List<App>();

    public Topic()
    {
    }

    public Topic(string code, string name, List<App> apps)
    {
        Code = code;
        Name = name;
        Apps = apps;
    }
}

public partial class Topic
{
    internal string TagId() => $"topic-{Code}";

    internal string TagStyle { get; set; }

    internal List<StringNumber> BindValues { get; set; }
}

public class App
{
    public string Code { get; set; }

    public string Name { get; set; }

    public int Sort { get; set; }

    public List<NavModel> Navs { get; set; } = new List<NavModel>();

    public string TagId(string topic) => $"topic-{topic}-app-{Code}";

    public App()
    {
    }

    public App(string code, string name, List<NavModel> navs)
    {
        Code = code;
        Name = name;
        Navs = navs;
    }

    public App(string code, string name, int sort, List<NavModel> navs) : this(code, name, navs)
    {
        Sort = sort;
    }
}