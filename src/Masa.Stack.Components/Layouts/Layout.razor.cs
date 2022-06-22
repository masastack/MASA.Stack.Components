using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;

namespace Masa.Stack.Components;

public partial class Layout
{
    [Inject]
    private I18n I18n { get; set; } = null!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter, EditorRequired]
    public string? Logo { get; set; }

    [Parameter, EditorRequired]
    public string? MiniLogo { get; set; }

    [Parameter, EditorRequired]
    public string AppId { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string? UserCenterRoute { get; set; }

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

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        logger.LogInformation("URL of new location: {Location}", e.Location);
        authClient.UserService.VisitedAsync(e.Location);
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
    }
}
