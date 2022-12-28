namespace Masa.Stack.Components;

public partial class SLayout
{
    [Inject]
    [NotNull]
    public IPopupService PopupService { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private I18n I18n { get; set; } = null!;

    [Inject]
    private MasaUser MasaUser { get; set; } = null!;

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
    public bool ShowBreadcrumbs { get; set; }

    [Parameter]
    public Func<bool>? OnSignOut { get; set; }

    [Parameter]
    public Func<Exception, Task>? OnErrorAsync { get; set; }

    [Parameter]
    public RenderFragment<Exception>? ErrorContent { get; set; }

    [Parameter]
    public List<string> WhiteUris { get; set; } = new List<string>();

    List<Nav> NavItems = new();
    List<string> _preWhiteUris = new();
    List<Nav> FlattenedNavs { get; set; } = new();
    List<Nav> FlattenedAllNavs { get; set; } = new();
    bool _noUserLogoutConfirm;
    List<string> _whiteUriList = new List<string> { "403", "404", "user-center",
        "notification-center", "notification-center/*" };

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (WhiteUris.Any() && !WhiteUris.SequenceEqual(_preWhiteUris))
        {
            _preWhiteUris = WhiteUris;
            _whiteUriList.AddRange(WhiteUris);
        }
    }

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
                Logger.LogError(e, "AuthClient.PermissionService.GetMenusAsync");
            }

            NavItems = menus.Adapt<List<Nav>>();

#if DEBUG
            if (Debugger.IsAttached && !NavItems.Any())
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
                        new Nav("simpleModalExample", "simpleModal", "/simpleModalExample", 2, "father"),
                    }),
                };
            }
#endif

            GlobalConfig.Menus = NavItems;

            //add home index content sould remove this code
            if (NavigationManager.Uri == NavigationManager.BaseUri)
            {
                NavigationManager.NavigateTo(NavItems.GetDefaultRoute());
                return;
            }
            var relativeUri = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "");
            if (!IsMenusUri(NavItems, relativeUri))
            {
                NavigationManager.NavigateTo("/403");
                return;
            }

            Logger.LogInformation("URL of navigation to : {Location}", NavigationManager.Uri);
            try
            {
                await AuthClient.UserService.VisitedAsync(AppId, relativeUri);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "AuthClient.UserService.VisitedAsync OnAfterRenderAsync");
            }


            FlattenedNavs = FlattenNavs(NavItems, true);
            FlattenedAllNavs = FlattenNavs(NavItems, false);

            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
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
                foreach (var child in nav.Children)
                {
                    child.ParentCode = nav.Code;
                }
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

        ErrorContent ??= Exception => builder => { };

        PopupService.ConfigToast(config =>
        {
            config.Position = ToastPosition.TopLeft;
        });
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var uri = e.Location;
        var relativeUri = uri.Replace(NavigationManager.BaseUri, "");
        if (uri.Contains("/dashboard") is false && !IsMenusUri(NavItems, relativeUri))
        {
            NavigationManager.NavigateTo("/403");
            return;
        }

        Logger.LogInformation("URL of new location: {Location}", e.Location);
        try
        {
            AuthClient.UserService.VisitedAsync(AppId, relativeUri);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AuthClient.UserService.VisitedAsync");
        }
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

    private async Task<bool> ErrorHandleAsync(Exception exception)
    {
        //todo handler caller return NoUserException
        if (exception.Message == "current_user_not_found")
        {
            _noUserLogoutConfirm = true;
            return true;
        }

        if (OnErrorAsync != null)
        {
            await OnErrorAsync.Invoke(exception);
        }

        return true;
    }
}
