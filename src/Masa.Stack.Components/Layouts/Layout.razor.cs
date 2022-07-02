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

    [Parameter, EditorRequired]
    public string McSignalRUrl { get; set; } = string.Empty;

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
            var menus = await AuthClient.PermissionService.GetMenusAsync(AppId);
            NavItems = menus.Adapt<List<Nav>>();
            if (!NavItems.Any())
            {
                //todo delete
                NavItems = new List<Nav>()
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
                        new Nav("uploadImageExample", "uploadImage", "/uploadImageExample", 2, "father"),
                        new Nav("comboxExample", "combox", "/comboxExample", 2, "father"),
                        new Nav("paginationSelectExample", "paginationSelect", "/paginationSelectExample", 2, "father")
                    }),
                };
            }
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
        AuthClient.UserService.VisitedAsync(e.Location);
    }

    private async Task AddFavoriteMenu(string code)
    {
        await AuthClient.PermissionService.AddFavoriteMenuAsync(Guid.Parse(code));
    }

    private async Task RemoveFavoriteMenu(string code)
    {
        await AuthClient.PermissionService.RemoveFavoriteMenuAsync(Guid.Parse(code));
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
    }
}
