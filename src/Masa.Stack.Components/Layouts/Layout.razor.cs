using Masa.Stack.Components.Models;

namespace Masa.Stack.Components;

public partial class Layout : IDisposable
{
    /// <summary>
    /// @Body
    /// </summary>
    [Parameter, EditorRequired] 
    public RenderFragment? ChildContent { get; set; }

    [Parameter, EditorRequired]
    public string? DefaultRoute { get; set; }

    [Parameter, EditorRequired] 
    public string? Logo { get; set; }

    [Parameter, EditorRequired]
    public string? MiniLogo { get; set; }

    protected override void OnInitialized()
    {
        GlobalConfig.OnPageModeChanged += base.StateHasChanged;

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!Navs.Any())
        {
            Navs = await FetchSystemNavs();

            FlattenedNavs = FlattenNavs(Navs);

            StateHasChanged();
        }
    }

    private List<NavModel> Navs { get; set; } = new();

    private List<PageTabItem> PageTabItems =>
        FlattenedNavs.Where(n => n.Url is not null)
                     .Select(nav => new PageTabItem(nav.Name, nav.Url, nav.Icon))
                     .ToList();

    private List<NavModel> FlattenedNavs { get; set; } = new();

    private List<NavModel> FlattenNavs(List<NavModel> tree)
    {
        var res = new List<NavModel>();

        foreach (var nav in tree)
        {
            res.Add(nav);

            if (nav.Children is not null)
            {
                res.AddRange(FlattenNavs(nav.Children));
            }
        }

        return res;
    }

    private async Task<List<NavModel>> FetchSystemNavs()
    {
        return new List<NavModel>()
        {
            new NavModel("dashboard", "Dashboard", "mdi-view-dashboard-outline", "/", 1),
            new NavModel("counter", "Counter", "mdi-pencil", "/counter", 1),
            new NavModel("fetchdata", "Fetch data", "mdi-delete", "/fetchdata", 1),
            new NavModel("father", "Father", "mdi-numeric-0-box-outline", 1, new List<NavModel>
            {
                new NavModel("children", "ChildOne", "mdi-numeric-1-box-outline", "/has-children", 2, "father")
            }),
        };
    }

    public void Dispose()
    {
        GlobalConfig.OnPageModeChanged -= base.StateHasChanged;
    }
}