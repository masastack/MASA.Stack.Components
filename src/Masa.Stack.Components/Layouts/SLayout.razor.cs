namespace Masa.Stack.Components;

public partial class SLayout
{
    [Inject]
    [NotNull]
    public IPopupService PopupService { get; set; }

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private I18n I18n { get; set; } = null!;

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Style { get; set; }

    [Parameter]
    public string? TeamRouteFormat { get; set; }

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

    List<string> _whiteUriList = new List<string> { "403", "404", "", "/", "user-center",
        "notification-center", "notification-center/*" };

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

            if (!string.IsNullOrWhiteSpace(TeamRouteFormat))
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
                            Url = string.Format(TeamRouteFormat, team.Id),
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
                    new Nav("dashboard", "Dashboard", "mdi-view-dashboard-outline", "/index", 1),
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
                        new Nav("defaultTextFieldExample", "defaultTextField", "/defaultTextFieldExample", 2, "father"),
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

            GlobalConfig.Menus = NavItems;

            var uri = NavigationManager.Uri;
            //add home index content sould remove this code
            if (NavigationManager.Uri == NavigationManager.BaseUri)
            {
                NavigationManager.NavigateTo(HomeUri(NavItems));
                return;
            }

            if (!IsMenusUri(NavItems, uri.Replace(NavigationManager.BaseUri, "")))
            {
                NavigationManager.NavigateTo("/403");
                return;
            }

            FlattenedNavs = FlattenNavs(NavItems, true);
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private string HomeUri(List<Nav> navs)
    {
        var firstMenu = navs.FirstOrDefault();
        if (firstMenu != null)
        {
            if (string.IsNullOrEmpty(firstMenu.Url))
            {
                return HomeUri(firstMenu.Children);
            }

            return firstMenu.Url;
        }

        return "/";
    }
    private bool IsMenusUri(List<Nav> navs, string uri)
    {
        uri = uri.ToLower();
        if (_whiteUriList.Any(item => Regex.IsMatch(uri.ToLower(),
                Regex.Escape(item.ToLower()).Replace(@"\*", ".*"))))
        {
            return true;
        }

        var allowed = navs.Any(n => (n.Url ?? "").Trim('/').ToLower().Equals(uri));
        if (!allowed)
        {
            foreach (var nav in navs)
            {
                if (nav.HasChildren)
                {
                    allowed = IsMenusUri(nav.Children, uri);
                }

                if (allowed)
                {
                    break;
                }
            }
        }

        return allowed;
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
        var uri = e.Location;
        if (!IsMenusUri(NavItems, uri.Replace(NavigationManager.BaseUri, "")))
        {
            NavigationManager.NavigateTo("/403");
            return;
        }

        Logger.LogInformation("URL of new location: {Location}", e.Location);
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
