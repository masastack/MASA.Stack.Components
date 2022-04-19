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

    private List<Nav> Navs { get; set; } = new();

    private List<PageTabItem> PageTabItems =>
        FlattenedNavs.Where(n => n.Url is not null)
                     .Select(nav => new PageTabItem(nav.Name, nav.Url, nav.Icon))
                     .ToList();

    private List<Nav> FlattenedNavs { get; set; } = new();

    private List<Nav> FlattenNavs(List<Nav> tree)
    {
        var res = new List<Nav>();

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

    private async Task<List<Nav>> FetchSystemNavs()
    {
        return new List<Nav>()
        {
            new Nav("dashboard", "Dashboard", "mdi-view-dashboard-outline", "/", 1),
            new Nav("counter", "Counter", "mdi-pencil", "/counter", 1),
            new Nav("fetchdata", "Fetch data", "mdi-delete", "/fetchdata", 1),
            new Nav("father", "Father", "mdi-numeric-0-box-outline", 1, new List<Nav>
            {
                new Nav("children", "ChildOne", "mdi-numeric-1-box-outline", "/has-children", 2, "father"),
                new Nav("dialog", "dialog", "mdi-numeric-1-box-outline", "/dialog", 2, "father"),
                new Nav("tab", "tab", "mdi-numeric-1-box-outline", "/tab", 2, "father"),
                new Nav("mini", "mini", "mdi-numeric-1-box-outline", "/mini-components", 2, "father"),
                new Nav("extend", "extend", "mdi-numeric-1-box-outline", "/extend", 2, "father")
            }),
        };
    }

    public void Dispose()
    {
        GlobalConfig.OnPageModeChanged -= base.StateHasChanged;
    }
}