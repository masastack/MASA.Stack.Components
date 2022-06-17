using Mapster;

namespace Masa.Stack.Components;

public partial class Layout
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter, EditorRequired]
    public string? DefaultRoute { get; set; }

    [Parameter, EditorRequired]
    public string? Logo { get; set; }

    [Parameter, EditorRequired]
    public string? MiniLogo { get; set; }

    [Parameter, EditorRequired]
    public string AppId { get; set; } = string.Empty;

    List<Nav> NavItems = new();

    List<Nav> FlattenedNavs { get; set; } = new();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var menus = await authClient.PermissionService.GetMenusAsync(AppId);
            NavItems = menus.Adapt<List<Nav>>();
            FlattenedNavs = FlattenNavs(NavItems, true);
            StateHasChanged();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private List<Nav> FlattenNavs(List<Nav> tree, bool excludeNavHasChildren = false)
    {
        var res = new List<Nav>();

        foreach (var nav in tree)
        {
            if (!(nav.HasChildren && excludeNavHasChildren))
            {
                res.Add(nav);
            }

            if (nav.HasChildren)
            {
                res.AddRange(FlattenNavs(nav.Children, excludeNavHasChildren));
            }
        }

        return res;
    }
}
