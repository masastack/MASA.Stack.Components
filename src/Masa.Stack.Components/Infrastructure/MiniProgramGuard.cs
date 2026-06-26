namespace Masa.Stack.Components.Infrastructure;

internal class MiniProgramGuard
{
    public const string BlockSourceQueryKey = "source";
    public const string BlockSourceMiniProgram = "miniprogram";

    private readonly SappNavigationContext _sappNavigationContext;
    private readonly ISappClient _sappClient;
    private readonly MicroFrontendNavigationManager _navigationManager;
    private readonly I18n _i18n;
    private readonly ProjectAppOptions _projectApp;
    private readonly IMultiEnvironmentMasaStackConfig _multiEnvironmentMasaStackConfig;
    private readonly IMultiEnvironmentUserContext _multiEnvironmentUserContext;
    private readonly MiniProgramAccessState _accessState;

    public MiniProgramGuard(
        SappNavigationContext sappNavigationContext,
        ISappClient sappClient,
        MicroFrontendNavigationManager navigationManager,
        I18n i18n,
        ProjectAppOptions projectApp,
        IMultiEnvironmentMasaStackConfig multiEnvironmentMasaStackConfig,
        IMultiEnvironmentUserContext multiEnvironmentUserContext,
        MiniProgramAccessState accessState)
    {
        _sappNavigationContext = sappNavigationContext;
        _sappClient = sappClient;
        _navigationManager = navigationManager;
        _i18n = i18n;
        _projectApp = projectApp;
        _multiEnvironmentMasaStackConfig = multiEnvironmentMasaStackConfig;
        _multiEnvironmentUserContext = multiEnvironmentUserContext;
        _accessState = accessState;
    }

    /// <summary>
    /// 当前是否位于小程序拦截专用页面（/maintenance 或带 source=miniprogram 的 /403）。
    /// 普通权限 403 不在此列，仍保留完整布局与菜单。
    /// </summary>
    public bool IsOnMiniProgramBlockPage()
    {
        var path = NormalizeUri(_navigationManager.OriginalNavigationManager.GetAbsolutePath()).TrimEnd('/');
        if (path == "/maintenance")
            return _accessState.IsBlocked;

        if (path == "/403")
            return HasMiniProgramBlockSource(_navigationManager.Uri);

        return false;
    }

    public string ResolveAppId()
    {
        if (!_sappNavigationContext.AppIdOverride.IsNullOrEmpty())
            return _sappNavigationContext.AppIdOverride;

        return _multiEnvironmentMasaStackConfig
            .SetEnvironment(_multiEnvironmentUserContext.Environment ?? "")
            .GetWebId(_projectApp.Project);
    }

    public async Task ValidateAndRedirectAsync()
    {
        if (!_sappNavigationContext.UseSappNav)
            return;

        var appId = ResolveAppId();
        var module = await _sappClient.ModuleService.GetByPmIdentityAsync(appId);
        _accessState.SetFromModule(module);

        if (!IsOnMiniProgramBlockPage() && _accessState.IsBlocked)
            RedirectToBlockedPage();
    }

    public void RedirectToBlockedPage()
    {
        if (!_accessState.IsBlocked)
            return;

        switch (_accessState.Status)
        {
            case MiniProgramAccessStatus.NotFound:
                _navigationManager.NavigateTo(BuildForbiddenUrl(_i18n.T("MiniProgramNotFound")));
                break;
            case MiniProgramAccessStatus.NotListed:
                _navigationManager.NavigateTo(BuildForbiddenUrl(_i18n.T("MiniProgramNotListed")));
                break;
            case MiniProgramAccessStatus.Maintenance when _accessState.Module != null:
                _navigationManager.NavigateTo(BuildMaintenanceUrl(_accessState.Module));
                break;
        }
    }

    private static string BuildForbiddenUrl(string message) =>
        $"/403?message={Uri.EscapeDataString(message)}&{BlockSourceQueryKey}={BlockSourceMiniProgram}";

    private static string BuildMaintenanceUrl(ModuleDetailDto module)
    {
        var query = new List<string>();
        var maintaince = module.Maintenance!;
        if (!string.IsNullOrWhiteSpace(maintaince?.Reason))
            query.Add($"reason={Uri.EscapeDataString(maintaince.Reason)}");
        if (maintaince?.StartTime != default)
            query.Add($"start={Uri.EscapeDataString(maintaince.StartTime.ToString("O"))}");
        if (maintaince?.EndTime != default)
            query.Add($"end={Uri.EscapeDataString(maintaince.EndTime.ToString("O"))}");

        return query.Count == 0 ? "/maintenance" : $"/maintenance?{string.Join("&", query)}";
    }

    private string NormalizeUri(string uri)
    {
        if (uri.StartsWith(_navigationManager.ProjectPrefix, StringComparison.OrdinalIgnoreCase))
            return "/" + uri.Substring(_navigationManager.ProjectPrefix.Length);

        return uri;
    }

    private static bool HasMiniProgramBlockSource(string uri)
    {
        var queryIndex = uri.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex < 0)
            return false;

        foreach (var segment in uri[(queryIndex + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = segment.Split('=', 2);
            if (parts.Length == 2
                && parts[0].Equals(BlockSourceQueryKey, StringComparison.OrdinalIgnoreCase)
                && Uri.UnescapeDataString(parts[1]).Equals(BlockSourceMiniProgram, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
