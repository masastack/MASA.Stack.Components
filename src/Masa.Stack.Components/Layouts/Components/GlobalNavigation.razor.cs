using BlazorComponent;

namespace Masa.Stack.Components.Layouts;

public partial class GlobalNavigation
{
    [Parameter, EditorRequired]
    public RenderFragment<ActivatorProps> ActivatorContent { get; set; } = null!;

    private bool _visible;
    private List<Topic> Topics = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Topics = new List<Topic>()
            {
                new Topic("basic-ability","Basic Ability 基础能力", new List<App>()
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
                    new App("pm","Project Mangement 项目管理", new List<NavModel>()
                    {
                        new NavModel("all", "All views", "/all-view", 1),
                        new NavModel("groups", "Groups", "mdi-users", 1, new List<NavModel>()
                        {
                            new NavModel("group-1", "Group Name 1", "/group/1", 2),
                            new NavModel("group-2", "Group Name 2", "/group/2", 2),
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
                    new App("pm2","Project Mangement 项目管理", new List<NavModel>()
                    {
                        new NavModel("all", "All views", "/all-view", 1),
                        new NavModel("groups", "Groups", "mdi-users", 1, new List<NavModel>()
                        {
                            new NavModel("group-1", "Group Name 1", "/group/1", 2),
                            new NavModel("group-2", "Group Name 2", "/group/2", 2),
                        })
                    }),
                }),
                new Topic("basic-ability","Basic Ability 基础能力", new List<App>()
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
                    new App("pm","Project Mangement 项目管理", new List<NavModel>()
                    {
                        new NavModel("all", "All views", "/all-view", 1),
                        new NavModel("groups", "Groups", "mdi-users", 1, new List<NavModel>()
                        {
                            new NavModel("group-1", "Group Name 1", "/group/1", 2),
                            new NavModel("group-2", "Group Name 2", "/group/2", 2),
                        })
                    }),
                }),
            };

            StateHasChanged();
        }
    }
}


public class Topic
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

public class App
{
    public string Code { get; set; }

    public string Name { get; set; }

    public int Sort { get; set; }

    public List<NavModel> Navs { get; set; } = new List<NavModel>();

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