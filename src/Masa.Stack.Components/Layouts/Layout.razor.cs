using Masa.BuildingBlocks.BasicAbility.Auth.Contracts.Model;

namespace Masa.Stack.Components;

public partial class Layout
{
    [Inject]
    [NotNull]
    public IPopupService PopupService { get; set; }

    [Inject]
    private I18n I18n { get; set; } = null!;

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Style { get; set; }

    [Parameter]
    public string? TeamRoute { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter, EditorRequired]
    public string? Logo { get; set; }

    [Parameter, EditorRequired]
    public string? MiniLogo { get; set; }

    [Parameter, EditorRequired]
    public string AppId { get; set; } = string.Empty;

    [Parameter]
    public Func<bool>? OnSignOut { get; set; }

    [Parameter]
    public Func<Exception, Task>? OnErrorAsync { get; set; }

    [Parameter]
    public RenderFragment<Exception>? ErrorContent { get; set; }

    List<Nav> NavItems = new();

    List<Nav> FlattenedNavs { get; set; } = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            List<MenuModel> menus = new();

            try
            {
                menus = await AuthClient.PermissionService.GetMenusAsync(AppId);
            }
            catch (Exception e)
            {
                await PopupService.ToastErrorAsync(e.Message);
            }

            NavItems = menus.Adapt<List<Nav>>();

            if (!string.IsNullOrWhiteSpace(TeamRoute))
            {
                try
                {
                    var teams = await AuthClient.TeamService.GetUserTeamsAsync();
                    var teamNav = new Nav("stack.team", "Team", "mdi-account-group-outline", "", 0);
                    foreach (var team in teams)
                    {
                        var newNavItem = new Nav()
                        {
                            Code = $"{team.Id}",
                            Name = team.Name,
                            Icon = "mdi-circle",
                            ParentCode = teamNav.Code,
                            Url = string.Format(TeamRoute, team.Id),
                        };
                        teamNav.Children.Add(newNavItem);
                    }
                    if (teams.Any())
                    {
                        NavItems.Add(teamNav);
                    }
                }
                catch (Exception e)
                {
                    await PopupService.ToastErrorAsync(e.Message);
                }
            }

#if DEBUG
            if (!NavItems.Any())
            {
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
                        new Nav("paginationSelectExample", "paginationSelect", "/paginationSelectExample", 2, "father"),
                        new Nav("dateRangePickerExample", "dateRangePicker", "/dateRangePickerExample", 2, "father"),
                        new Nav("dateTimeRangePickerExample", "dateTimeRangePicker", "/dateTimeRangePickerExample", 2, "father"),
                    }),
                };
            }
#endif

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
        OnErrorAsync ??= async exception =>
        {
            await PopupService.ToastErrorAsync(exception.Message);
        };

        PopupService.ConfigToast(config =>
        {
            config.Position = ToastPosition.TopLeft;
        });

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
