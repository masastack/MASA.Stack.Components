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

    [Inject]
    public JsInitVariables JsInitVariables { get; set; } = default!;

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

    [Parameter]
    public bool Exact { get; set; } = true;

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
            await JsInitVariables.SetTimezoneOffset();
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
                    new Nav("dashboard", "Dashboard", "mdi-view-dashboard-outline", "/index"),
                    new Nav("counter", "Counter", "mdi-pencil", "/counter"),
                    new Nav("fetchdata", "Fetch data", "mdi-delete", "/fetchdata"),
                    new Nav("father", "Father", "mdi-numeric-0-box-outline", new List<Nav>
                    {
                        new Nav("children2", "ChildTwo", "father", new List<Nav>()
                        {
                            new Nav("children", "ChildOne", "/has-children", "children2"),
                        }),
                        new Nav("dialog", "dialog",default, "/dialog", "father"),
                        new Nav("tab", "tab", default,"/tab", "father"),
                        new Nav("sselect", "sselect", default,"/sselect", "father"),
                        new Nav("mini", "mini",default, "/mini-components", "father"),
                        new Nav("extend", "extend", default,"/extend", "father"),
                        new Nav("userAutoCompleteExample", "userAutoComplete",default, "/userAutoCompleteExample", "father"),
                        new Nav("defaultTextFieldExample", "defaultTextField",default, "/defaultTextFieldExample", "father"),
                        new Nav("defaultButtonExample", "defaultButton",default, "/defaultButtonExample", "father"),
                        new Nav("defaultDataTableExample", "defaultDataTable",default, "/defaultDataTableExample", "father"),
                        new Nav("paginationExample", "pagination", default,"/defaultPaginationExample", "father"),
                        new Nav("uploadImageExample", "uploadImage",default, "/uploadImageExample", "father"),
                        new Nav("comboxExample", "combox", default,"/comboxExample", "father"),
                        new Nav("paginationSelectExample", "paginationSelect",default, "/paginationSelectExample", "father"),
                        new Nav("dateRangePickerExample", "dateRangePicker",default, "/dateRangePickerExample", "father"),
                        new Nav("dateTimeRangePickerExample", "dateTimeRangePicker", default,"/dateTimeRangePickerExample", "father"),
                        new Nav("simpleModalExample", "simpleModal", default,"/simpleModalExample", "father"),
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
            if (relativeUri.Contains("dashboard") is false && !IsMenusUri(NavItems, relativeUri))
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
        if (exception is UserStatusException)
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
