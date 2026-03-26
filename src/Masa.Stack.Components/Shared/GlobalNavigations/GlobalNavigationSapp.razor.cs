namespace Masa.Stack.Components;

public partial class GlobalNavigationSapp : MasaComponentBase
{
    private const string MENU_URL_NAME = "url";
    private const string MENU_ID_NAME = "id";
    private const string MENU_ICON_NAME = "icon";
    private const string MENU_NAVIGATION_TYPE_NAME = "navigationType";
    private const string MENU_OPEN_TYPE_NAME = "openType";
    private const string MENU_MODULE_ID_NAME = "moduleId";
    private const string MENU_PERMISSION_ID_NAME = "permissionId";
    private const string MENU_SORT_NAME = "sort";
    private const string MENU_IS_HIDDEN_NAME = "isHidden";
    private const string APP_STATUS_NAME = "status";

    [Parameter]
    public string ClientId { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment<ActivatorProps> ActivatorContent { get; set; } = null!;

    [Parameter]
    public Func<string, Task>? OnFavoriteAdd { get; set; }

    [Parameter]
    public Func<string, Task>? OnFavoriteRemove { get; set; }

    [Inject]
    public GlobalConfig GlobalConfig { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    private bool _visible;
    private List<(string name, string url)>? _recentVisits;
    private List<AppEntryDto>? _visibleAppEntries;
    private List<ExpansionMenu>? _favorites;
    private ExpansionMenu? _menu;
    private string? _search;

    private async Task GetMenuAndFavorites()
    {
        _menu = await GenerateMenuAsync();
        _favorites = _menu.GetMenusByStates(ExpansionMenuState.Favorite);
        StateHasChanged();
    }

    private void VisibleChanged(bool visible)
    {
        if (visible)
        {
            _search = string.Empty;
            OnEnter();
            if (_menu == null)
            {
                _ = GetRecentVisits();
                _ = GetMenuAndFavorites();
            }

            if (_visibleAppEntries == null)
            {
                _ = GetVisibleAppEntriesAsync();
            }
        }

        _visible = visible;
    }

    private void OnEnter()
    {
        _menu?.SetHiddenBySearch(_search, TranslateProvider);
    }

    private async Task MenuItemClickAsync(ExpansionMenu menu)
    {
        var url = menu.GetData(MENU_URL_NAME);
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (ShouldOpenInNewWindow(menu))
        {
            var absoluteUrl = BuildAbsoluteUrl(url);
            await JsRuntime.InvokeVoidAsync("open", absoluteUrl, "_blank");
            if (ShouldCloseDialog(absoluteUrl))
            {
                _visible = false;
            }
            return;
        }

        NavigateTo(url);
    }

    private async Task MenuItemOperClickAsync(ExpansionMenu menu)
    {
        if (!IsFavoriteSupported(menu))
        {
            return;
        }

        if (menu.State == ExpansionMenuState.Normal)
        {
            await FavoriteRemoveAsync(menu);
        }
        else if (menu.State == ExpansionMenuState.Favorite)
        {
            await FavoriteAddAsync(menu);
        }
    }

    private async Task<ExpansionMenu> GenerateMenuAsync()
    {
        var menu = ExpansionMenu.CreateRootMenu(ExpansionMenuSituation.Favorite);
        try
        {
            var apps = await SappClient.GlobalNavService.GetGlobalNavigationsByClientIdAsync(ClientId);
            var categories = apps.GroupBy(a => a.Tag).ToList();
            var favorites = await GlobalNavigationInteractionHelper.FetchFavoritesAsync(AuthClient);

            foreach (var category in categories)
            {
                var categoryMenu = new ExpansionMenu(category.Key, category.Key, ExpansionMenuType.Category, ExpansionMenuState.Normal, menu.MetaData,
                    parent: menu);
                foreach (var app in category.Where(a => a.Navs.Any()))
                {
                    var appMenu = new ExpansionMenu(app.Id.ToString(), app.Name, ExpansionMenuType.App, ExpansionMenuState.Normal, menu.MetaData,
                        parent: categoryMenu)
                        .AddData(MENU_ICON_NAME, app.Icon)
                        .AddData(APP_STATUS_NAME, app.Status.ToString());
                    var sort = 1;
                    foreach (var nav in app.Navs)
                    {
                        appMenu.AddChild(ConvertForNav(nav, appMenu.Deep + 1, appMenu, app.Id, sort++, favorites));
                    }

                    categoryMenu.AddChild(appMenu);
                }

                menu.AddChild(categoryMenu);
            }
        }
        catch (Exception ex)
        {
            // Keep the popover renderable even when remote data retrieval fails.
            Logger.LogWarning(ex, "Sapp global navigation load failed.");
        }

        return menu;
    }

    private async Task GetVisibleAppEntriesAsync()
    {
        _visibleAppEntries = (await SappClient.GlobalNavService.GetVisibleAppEntriesByClientIdAsync(ClientId))
            .ToList();

        StateHasChanged();
    }

    private static ExpansionMenu ConvertForNav(GlobalNavigationNodeDto navModel, int deep, ExpansionMenu parent, Guid moduleId, int sort, List<string> favorites)
    {
        var favoriteId = ResolveFavoriteId(navModel);
        var hasFavoriteId = navModel.NavigationType == GlobalNavigationTypes.Normal && favoriteId != Guid.Empty;
        var state = hasFavoriteId && favorites.Any(favorite => favorite == favoriteId.ToString())
            ? ExpansionMenuState.Favorite
            : ExpansionMenuState.Normal;
        var menuId = hasFavoriteId
            ? favoriteId.ToString()
            : navModel.Id?.ToString() ?? navModel.Code;

        var menu = new ExpansionMenu(menuId, navModel.Name, ExpansionMenuType.Nav, state, parent.MetaData, parent: parent)
            .AddData(MENU_ID_NAME, navModel.Id?.ToString() ?? string.Empty)
            .AddData(MENU_URL_NAME, navModel.Url)
            .AddData(MENU_ICON_NAME, navModel.Icon)
            .AddData(MENU_NAVIGATION_TYPE_NAME, navModel.NavigationType.ToString())
            .AddData(MENU_OPEN_TYPE_NAME, navModel.OpenType.ToString())
            .AddData(MENU_MODULE_ID_NAME, moduleId.ToString())
            .AddData(MENU_PERMISSION_ID_NAME, navModel.PermissionId == Guid.Empty ? string.Empty : navModel.PermissionId.ToString())
            .AddData(MENU_SORT_NAME, sort.ToString())
            .AddData(MENU_IS_HIDDEN_NAME, navModel.IsHidden.ToString());
        var childSort = 1;
        foreach (var childrenNav in navModel.Children)
        {
            menu.AddChild(ConvertForNav(childrenNav, deep++, menu, moduleId, childSort++, favorites));
        }

        menu.Disabled = menu.Children.Count > 0;
        return menu;
    }

    private static Guid ResolveFavoriteId(GlobalNavigationNodeDto navModel)
    {
        var permissionId = navModel.PermissionId;
        if (permissionId != Guid.Empty)
        {
            return permissionId;
        }

        var id = navModel.Id;
        if (id.HasValue && id.Value != Guid.Empty)
        {
            return id.Value;
        }

        return Guid.Empty;
    }

    private async Task GetRecentVisits()
    {
        _recentVisits = await GlobalNavigationInteractionHelper.FetchRecentVisitsAsync(AuthClient);

        StateHasChanged();
    }

    private void NavigateTo(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (ShouldCloseDialog(url))
        {
            _visible = false;
        }

        NavigationManager.NavigateTo(url);
    }

    private static bool HasWebFullIcon(AppEntryDto app)
    {
        return !string.IsNullOrWhiteSpace(app.WebFullIcon);
    }

    private static bool IsFavoriteSupported(ExpansionMenu menu)
    {
        return EnumHelper.TryParse(menu.GetData(MENU_NAVIGATION_TYPE_NAME), out GlobalNavigationTypes navigationType) &&
               navigationType == GlobalNavigationTypes.Normal;
    }

    private static bool ShouldOpenInNewWindow(ExpansionMenu menu)
    {
        return EnumHelper.TryParse(menu.GetData(MENU_OPEN_TYPE_NAME), out GlobalNavigationOpenTypes openType) &&
               openType == GlobalNavigationOpenTypes.NewWindow;
    }

    private string BuildAbsoluteUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return url;
        }

        var href = BuildHref(url);
        return NavigationManager.OriginalNavigationManager.ToAbsoluteUri(href).ToString();
    }

    private bool ShouldCloseDialog(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var targetUri))
        {
            return true;
        }

        var currentUri = NavigationManager.OriginalNavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        return string.Equals(targetUri.Host, currentUri.Host, StringComparison.OrdinalIgnoreCase);
    }

    private async Task FavoriteRemoveAsync(ExpansionMenu nav)
    {
        await GlobalNavigationInteractionHelper.RemoveFavoriteAsync(_favorites!, nav, OnFavoriteRemove);
    }

    private async Task FavoriteAddAsync(ExpansionMenu nav)
    {
        await GlobalNavigationInteractionHelper.AddFavoriteAsync(_favorites!, nav, OnFavoriteAdd);
    }

}
