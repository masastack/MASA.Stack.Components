using Masa.Stack.Components.Models;

namespace Masa.Stack.Components;

public partial class Layout
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
                     .Select(nav => new PageTabItem(nav.Name, nav.Url, nav.Icon ?? ""))
                     .ToList();

    private List<Nav> FlattenedNavs { get; set; } = new();

    private List<Nav> FlattenNavs(List<Nav> tree)
    {
        var res = new List<Nav>();

        foreach (var nav in tree)
        {
            res.Add(nav);
            res.AddRange(FlattenNavs(nav.Children));
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
                new Nav("children2", "ChildTwo", 2, "father", new List<Nav>()
                {
                    new Nav("children", "ChildOne", "/has-children", 3, "children2"),
                }),
                new Nav("dialog", "dialog", "/dialog", 2, "father"),
                new Nav("tab", "tab", "/tab", 2, "father"),
                new Nav("mini", "mini", "/mini-components", 2, "father"),
                new Nav("extend", "extend", "/extend", 2, "father"),
                new Nav("userAutoCompleteExample", "userAutoComplete", "/userAutoCompleteExample", 2, "father"),
                new Nav("defaultButtonExample", "defaultButton", "/defaultButtonExample", 2, "father"),
                new Nav("defaultDataTableExample", "defaultDataTable", "/defaultDataTableExample", 2, "father"),
                new Nav("paginationExample", "pagination", "/defaultPaginationExample", 2, "father"),
                new Nav("uploadImageExample", "uploadImage", "/uploadImageExample", 2, "father")
            }),
        };
    }
}
