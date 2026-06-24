namespace Masa.Stack.Components.Infrastructure.Identity;

/// <summary>
/// WASM 模式的团队状态管理器
/// 切换团队后清除浏览器中缓存的 OIDC Token，通过页面重载触发静默登录以获取包含最新团队信息的新 Token
/// </summary>
public class WasmTeamStateManager : ITeamStateManager, IScopedDependency
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<WasmTeamStateManager> _logger;

    public WasmTeamStateManager(
        NavigationManager navigationManager,
        IJSRuntime jsRuntime,
        ILogger<WasmTeamStateManager> logger)
    {
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task SetCurrentTeamAsync(Guid teamId)
    {
        try
        {
            _logger.LogInformation("开始切换团队，团队ID: {TeamId}", teamId);

            // 清除浏览器 SessionStorage 中缓存的 OIDC Token，
            // 使下次认证时必须从 SSO 重新获取包含最新 CURRENT_TEAM claim 的 Token
            await _jsRuntime.InvokeVoidAsync("MasaStackComponents.clearOidcTokenCache");

            // 强制刷新页面，框架发现无缓存 Token 后会走 OIDC 静默登录，SSO 颁发新 Token
            _navigationManager.NavigateTo(_navigationManager.Uri, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换团队时发生错误，团队ID: {TeamId}", teamId);
        }
    }
}
