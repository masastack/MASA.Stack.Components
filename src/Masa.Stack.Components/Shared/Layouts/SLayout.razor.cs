using Masa.Stack.Components.Infrastructure.SessionRevocation;

namespace Masa.Stack.Components;

public partial class SLayout
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; }

    [Inject]
    public IPopupService PopupService { get; set; } = null!;

    [Inject]
    public MicroFrontendNavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private I18n I18n { get; set; } = null!;

    [Inject]
    private SkinManager SkinManager { get; set; } = null!;

    internal string AppearanceId { get; } = "masa-appearance-" + Guid.NewGuid().ToString("N");

    [Inject]
    private MasaUser MasaUser { get; set; } = null!;

    [Inject]
    private IMultiEnvironmentMasaStackConfig MultiEnvironmentMasaStackConfig { get; set; } = null!;

    [Inject]
    private IMultiEnvironmentUserContext MultiEnvironmentUserContext { get; set; } = null!;

    [Inject]
    public JsInitVariables JsInitVariables { get; set; } = default!;

    [Inject]
    public I18nCache I18nCache { get; set; } = default!;

    [Inject]
    private ISappClient SappClient { get; set; } = null!;

    [Inject]
    private MiniProgramAccessState MiniProgramAccessState { get; set; } = null!;

    [Inject]
    private MiniProgramGuard MiniProgramGuard { get; set; } = null!;

    [Inject]
    private SappNavigationContext SappNavigationContext { get; set; } = null!;

    [Inject]
    private Extensions.OpenIdConnect.MasaOpenIdConnectOptions? OpenIdConnectOptions { get; set; }

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    /// 会话撤销探测（见 <see cref="SessionRevocationWatcher"/>）。只有宿主接入了 OIDC（<c>AddMasaOpenIdConnectAsync</c>）才有，
    /// 所以用 <c>GetService</c> 取而不是 <c>[Inject]</c>：没注册就静默跳过，不影响没接入 OIDC 的宿主。
    /// 生命周期归 DI（scoped），布局只负责启停。
    /// </summary>
    private SessionRevocationWatcher? _sessionRevocationWatcher;

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

    [Inject]
    internal ProjectAppOptions ProjectApp { get; set; } = default!;

    [Parameter]
    public Func<Task>? BeforeSignOut { get; set; }

    [Parameter]
    public Func<Exception, Task>? OnErrorAsync { get; set; }

    [Parameter]
    public RenderFragment<Exception>? ErrorContent { get; set; }

    [Parameter]
    public EventCallback<Exception> OnErrorAfterHandle { get; set; }

    [Parameter]
    public List<string> WhiteUris { get; set; } = new List<string>();

    [Parameter]
    public bool IsShowEnvironmentSwitch { get; set; } = false;

    [Parameter]
    public string BaseUri { get; set; } = "/";

    [Parameter]
    public bool UseSappNav { get; set; }

    /// <summary>微前端时面包屑最前的模块名（Stack 项目枚举名）。</summary>
    private string? BreadcrumbModuleName =>
        NavigationManager.IsMicroFrontend ? ProjectApp.Project.ToString() : null;

    private Breadcrumbs? _breadcrumbsComp;
    private Action? _breadcrumbSetCallback;

    private Breadcrumbs? BreadcrumbsComp
    {
        get => _breadcrumbsComp;
        set
        {
            _breadcrumbsComp = value;
            if (value is null)
            {
                return;
            }

            _breadcrumbSetCallback?.Invoke();
        }
    }

    [Parameter]
    public string? AppId { get; set; }

    private bool IsMiniProgramBlocked => UseSappNav && MiniProgramAccessState.IsBlocked;

    private bool UseMinimalLayout => IsMiniProgramBlocked && MiniProgramGuard.IsOnMiniProgramBlockPage();

    public string GetAppId() => MultiEnvironmentMasaStackConfig.SetEnvironment(MultiEnvironmentUserContext.Environment ?? "").GetWebId(ProjectApp.Project);

    List<Nav> NavItems = new();
    bool _navInit;
    // 首次渲染里"登录无关"的基础初始化（全局配置、皮肤、i18n）是否已完成；之后才允许补做导航初始化。
    bool _baseInitialized;
    bool _navInitRunning;
    List<string> _preWhiteUris = new();

    List<Nav> FlattenedNavs { get; set; } = new();

    List<Nav> FlattenedAllNavs { get; set; } = new();

    List<string> _whiteUriList = new List<string>
    {
        "403", "404", "maintenance", "user-center",
        "notification-center"
    };



    string ClientId
    {
        get
        {
            if (!AppId.IsNullOrEmpty())
            {
                return AppId;
            }

            return UseSappNav ? OpenIdConnectOptions?.ClientId ?? string.Empty : GetAppId();
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        SappNavigationContext.Update(UseSappNav, AppId);
        I18nCache.UseSappNav = UseSappNav;
        if (WhiteUris.Any() && !WhiteUris.SequenceEqual(_preWhiteUris))
        {
            _preWhiteUris = WhiteUris;
            _whiteUriList.AddRange(WhiteUris);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        Logger.LogInformation("SLayout OnAfterRenderAsync");
        if (firstRender)
        {
            await GlobalConfig.InitializationAsync();
            await SkinManager.InitializeAsync();
            await I18nCache.InitializeAsync();

            await JsInitVariables.SetTimezoneOffset();
            _baseInitialized = true;

            await TryInitializeNavAsync(redirectToLogin: true);
        }
        else if (_baseInitialized && !_navInit)
        {
            // 首次渲染时还没登录（新窗口 sessionStorage 为空，要先走 /authentication/login 的重定向/静默登录），
            // 导航初始化被跳过了。登录完成后布局并不会再次 firstRender，所以这里在后续渲染里补做，
            // 否则菜单与内容区会一直是空白（表现为"加载卡住"）。
            await TryInitializeNavAsync(redirectToLogin: false);
        }
    }

    /// <summary>
    /// 在已登录的前提下初始化导航（菜单、默认路由、访问记录）。幂等：已初始化或正在初始化时直接返回；
    /// 未登录时不做任何事，等登录完成后由后续渲染 / 路由变化再次触发。
    /// </summary>
    /// <param name="redirectToLogin">未登录时是否跳转到登录页（只有首次渲染需要，其余场景只是重试，不能重复跳转）。</param>
    private async Task TryInitializeNavAsync(bool redirectToLogin)
    {
        if (_navInit || _navInitRunning)
        {
            return;
        }

        _navInitRunning = true;
        try
        {
            if (!await CheckAuthenticatedAsync(redirectToLogin))
            {
                if (redirectToLogin)
                {
                    Logger.LogInformation("not CheckAuthenticated, navigation init deferred until authenticated");
                }

                return;
            }

            StartSessionRevocationWatcher();

            await InitializeNavCoreAsync();
        }
        finally
        {
            _navInitRunning = false;
        }
    }

    private async Task InitializeNavCoreAsync()
    {
        List<Nav> menus = new();

        if (UseSappNav)
        {
            try
            {
                await MiniProgramGuard.ValidateAndRedirectAsync();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "MiniProgramGuard.ValidateAndRedirectAsync");
            }
        }

        if (IsMiniProgramBlocked)
        {
            if (!MiniProgramGuard.IsOnMiniProgramBlockPage())
                MiniProgramGuard.RedirectToBlockedPage();

            _navInit = true;
            return;
        }

        try
        {
            var appId = MiniProgramGuard.ResolveAppId();
            if (UseSappNav)
            {
                var sappMenus = await SappClient.GlobalNavService.GetMenusByPmIdentityAsync(appId);
                menus = sappMenus.Adapt<List<Nav>>();
            }
            else
            {
                var authMenus = await AuthClient.PermissionService.GetMenusAsync(appId);
                menus = authMenus.Adapt<List<Nav>>();
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, UseSappNav
                ? "SappClient.GlobalNavService.GetMenusByPmIdentityAsync"
                : "AuthClient.PermissionService.GetMenusAsync");
        }

        NavItems = menus;

#if DEBUG
        if (Debugger.IsAttached && !NavItems.Any())
        {
            NavItems = new List<Nav>()
            {
                new Nav("dashboard", "Dashboard", "mdi-view-dashboard-outline", "/index"),
                new Nav("user", "User", "mdi-account", "/users"),
                new Nav("products", "Products", "mdi-rectangle", "/products/list", matchPattern: "/products/(list|details/)[^/]*"),
                new Nav("counter", "Counter", "mdi-pencil", "/counter"),
                new Nav("fetchdata", "Fetch data", "mdi-delete", "/fetchdata"),
                new Nav("father", "Father", null, new List<Nav>
                {
                    new Nav("children2", "ChildTwo", "father", new List<Nav>()
                    {
                        new Nav("children", "ChildOne", default, "/children2", "children2"),
                    }),
                    new Nav("dialog", "dialog", default, "/dialog", "father"),
                    new Nav("tab", "tab", default, "/tab", "father"),
                    new Nav("sselect", "sselect", default, "/sselect", "father"),
                    new Nav("deleteButton", "deleteButton", default, "/deleteButton", "father"),
                    new Nav("mini", "mini", default, "/mini-components", "father"),
                    new Nav("extend", "extend", default, "/extend", "father"),
                    new Nav("userAutoCompleteExample", "userAutoComplete", default, "/userAutoCompleteExample", "father"),
                    new Nav("defaultTextFieldExample", "defaultTextField", default, "/defaultTextFieldExample", "father"),
                    new Nav("defaultButtonExample", "defaultButton", default, "/defaultButtonExample", "father"),
                    new Nav("defaultDataTableExample", "defaultDataTable", default, "/defaultDataTableExample", "father"),
                    new Nav("paginationExample", "pagination", default, "/defaultPaginationExample", "father"),
                    new Nav("uploadImageExample", "uploadImage", default, "/uploadImageExample", "father"),
                    new Nav("comboxExample", "combox", default, "/comboxExample", "father"),
                    new Nav("paginationSelectExample", "paginationSelect", default, "/paginationSelectExample", "father"),
                    new Nav("dateRangePickerExample", "dateRangePicker", default, "/dateRangePickerExample", "father"),
                    new Nav("dateTimeRangePickerExample", "dateTimeRangePicker", default, "/dateTimeRangePickerExample", "father"),
                    new Nav("simpleModalExample", "simpleModal", default, "/simpleModalExample", "father"),
                    new Nav("simpleConfirmExample", "simpleConfirmExample", default, "/simpleConfirmExample", "father"),
                    new Nav("empty", "empty", default, "/empty", "father"),
                    new Nav("icons", "icons", default, "/icons", "father"),
                    new Nav("searchPanelExample", "searchPanelExample", default, "/searchPanelExample", "father"),
                }),
            };
        }
#endif

        GlobalConfig.Menus = NavItems;

        FlattenedNavs = FlattenNavs(NavItems, true);
        FlattenedAllNavs = FlattenNavs(NavItems, false);

        _navInit = true;

        //add home index content sould remove this code
        if (NavigationManager.Uri == NavigationManager.BaseUri)
        {
            NavigationManager.NavigateTo(NavItems.GetDefaultRoute(projectPrefix: NavigationManager.ProjectPrefix));
            return;
        }

        var absolutePath = NavigationManager.OriginalNavigationManager.GetAbsolutePath();
        if (absolutePath.Contains("dashboard") is false && !IsMenusUri(NavItems, absolutePath))
        {
            NavigationManager.NavigateTo("/403");
            return;
        }

        Logger.LogInformation("URL of navigation to : {Location}", NavigationManager.Uri);
        try
        {
            await AuthClient.UserService.VisitedAsync(AppId, absolutePath);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AuthClient.UserService.VisitedAsync OnAfterRenderAsync");
        }

        StateHasChanged();
    }

    /// <summary>
    /// Update the text of last breadcrumb
    /// </summary>
    /// <param name="text"></param>
    /// <example>
    /// UpdateLastBreadcrumb("New Text");
    /// </example>
    public void ReplaceLastBreadcrumb(string text)
    {
        if (BreadcrumbsComp is null)
        {
            _breadcrumbSetCallback = () => BreadcrumbsComp!.ReplaceLastBreadcrumb(text);
        }
        else
        {
            BreadcrumbsComp.ReplaceLastBreadcrumb(text);
        }
    }

    /// <summary>
    /// Update breadcrumbs
    /// </summary>
    /// <param name="configure"></param>
    /// <example>
    /// UpdateBreadcrumbs(items => {
    ///     items[0].Text = "New Text";
    ///     items[0].Url = "/new-url";
    /// })
    /// </example>
    public void UpdateBreadcrumbs(Action<List<BreadcrumbItem>> configure)
    {
        if (BreadcrumbsComp is null)
        {
            _breadcrumbSetCallback = () => BreadcrumbsComp!.UpdateBreadcrumbs(configure);
        }
        else
        {
            BreadcrumbsComp.UpdateBreadcrumbs(configure);
        }
    }

    private bool IsMenusUri(List<Nav> navs, string uri)
    {
        uri = NormalizeUri(uri);

        if (_whiteUriList.Any(w => Regex.IsMatch(uri.ToLower(), w, RegexOptions.IgnoreCase)))
        {
            return true;
        }

        var allowed = navs.Any(n => (n.Url ?? "").Equals(uri, StringComparison.OrdinalIgnoreCase));
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
                if (nav.HasChildren)
                {
                    nav.ChildUrl = nav.Children.FirstOrDefault(u => !u.HasChildren)?.Url;
                }

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
        OnErrorAsync ??= async exception => { await PopupService.EnqueueSnackbarAsync(exception, false); };

        ErrorContent ??= Exception => builder => { };

        NavigationManager.LocationChanged += HandleLocationChanged;

        I18nCache.OnSectionUpdated += HandleSectionUpdated;
    }

    private void HandleSectionUpdated()
    {
        InvokeAsync(StateHasChanged);
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // 事件处理器是同步签名，但 CheckAuthenticatedAsync 现在必须 await：
        // 用 async void 会吞异常，这里改成显式派发并统一兜底。
        _ = HandleLocationChangedAsync(e);
    }

    private async Task HandleLocationChangedAsync(LocationChangedEventArgs e)
    {
        try
        {
            var absolutePath = NavigationManager.OriginalNavigationManager.GetAbsolutePath();
            if (!await CheckAuthenticatedAsync())
            {
                return;
            }

            // Start 幂等：首次渲染时还没登录（如从 /authentication/* 回调页客户端路由进来）的情况在这里补启动；
            // 已启动时只是按节流追加一次探测。
            StartSessionRevocationWatcher();
            _sessionRevocationWatcher?.NotifyLocationChanged();

            // 首次渲染时未登录会跳到 /authentication/login 并放弃导航初始化；登录完成后回跳到应用页面就会走到这里，
            // 此时补做导航初始化（菜单、默认路由、访问记录），否则页面永远没有菜单和内容。
            if (_baseInitialized && !_navInit)
            {
                await TryInitializeNavAsync(redirectToLogin: false);
                return;
            }

            if (IsMiniProgramBlocked && !MiniProgramGuard.IsOnMiniProgramBlockPage())
            {
                MiniProgramGuard.RedirectToBlockedPage();
                return;
            }

            if (_navInit && absolutePath.Contains("/dashboard") is false && !IsMenusUri(NavItems, absolutePath) && !(absolutePath == NavigationManager.ProjectPrefix && NavItems.Any()))
            {
                NavigationManager.NavigateTo("/403");
                return;
            }

            Logger.LogInformation("URL of new location: {Location}", e.Location);
            try
            {
                // 原实现没有 await，导致下面的 catch 永远收不到异常（CS4014），
                // 访问统计失败会变成未观察的 Task 异常。这里改为真正等待。
                await AuthClient.UserService.VisitedAsync(AppId, absolutePath);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "AuthClient.UserService.VisitedAsync");
            }
        }
        catch (ObjectDisposedException)
        {
            // 导航离开后布局已释放，正常
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "SLayout.HandleLocationChanged");
        }
    }

    private async Task<bool> CheckAuthenticatedAsync(bool redirectToLogin = true)
    {
        var absolutePath = NavigationManager.OriginalNavigationManager.GetAbsolutePath();
        if (absolutePath.Contains("/authentication/"))
        {
            return false;
        }

        // 不能用 AuthenticationStateTask.Result：单线程 WASM 下若该 task 尚未完成就会硬挂整个 UI。
        // 模拟登录装饰器让 GetAuthenticationStateAsync 多了一次 localStorage 的 JS 往返，
        // 未完成窗口变长，同步阻塞尤其危险。
        var authState = await AuthenticationStateTask;
        if (authState.User.Identity?.IsAuthenticated != true)
        {
            if (redirectToLogin)
            {
                NavigationManager.NavigateTo(OidcClientConsts.LOGIN_PATH);
            }

            return false;
        }

        return true;
    }

    private async Task AddFavoriteMenu(string code)
    {
        if (!Guid.TryParse(code, out var menuId) || menuId == Guid.Empty)
        {
            return;
        }

        await AuthClient.PermissionService.AddFavoriteMenuAsync(menuId);
    }

    private async Task RemoveFavoriteMenu(string code)
    {
        if (!Guid.TryParse(code, out var menuId) || menuId == Guid.Empty)
        {
            return;
        }

        await AuthClient.PermissionService.RemoveFavoriteMenuAsync(menuId);
    }

    private async Task<bool> OnErrorHandleAsync(Exception exception)
    {
        if (OnErrorAsync != null)
        {
            await OnErrorAsync.Invoke(exception);
        }

        return true;
    }

    private void StartSessionRevocationWatcher()
    {
        try
        {
            _sessionRevocationWatcher ??= ServiceProvider.GetService<SessionRevocationWatcher>();
            _sessionRevocationWatcher?.Start();
        }
        catch (Exception e)
        {
            // 探测是附加能力：它的装配问题（如 Authority 配置不合法）不能拖垮布局。
            Logger.LogWarning(e, "SessionRevocationWatcher could not be started");
        }
    }

    public void Dispose()
    {
        _sessionRevocationWatcher?.Stop();
        NavigationManager.LocationChanged -= HandleLocationChanged;
    }

    private string NormalizeUri(string uri)
    {
        if (uri.StartsWith(NavigationManager.ProjectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return "/" + uri.Substring(NavigationManager.ProjectPrefix.Length);
        }
        return uri;
    }
}
