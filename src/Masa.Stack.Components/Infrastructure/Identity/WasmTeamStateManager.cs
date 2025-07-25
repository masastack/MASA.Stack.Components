using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Masa.Stack.Components.Infrastructure.Identity;

/// <summary>
/// WASM 模式的团队状态管理器
/// 在 WASM 模式下，通过 refresh token 获取包含最新团队信息的 token
/// </summary>
public class WasmTeamStateManager : ITeamStateManager, IScopedDependency
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly NavigationManager _navigationManager;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly ILogger<WasmTeamStateManager> _logger;

    public WasmTeamStateManager(
        AuthenticationStateProvider authenticationStateProvider,
        NavigationManager navigationManager,
        IAccessTokenProvider accessTokenProvider,
        ILogger<WasmTeamStateManager> logger)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _navigationManager = navigationManager;
        _accessTokenProvider = accessTokenProvider;
        _logger = logger;
    }

    /// <summary>
    /// 强制刷新 token，通过清除当前 token 来触发 refresh token 流程
    /// </summary>
    private async Task<string?> ForceRefreshTokenAsync()
    {
        try
        {
            // 通过请求新的 token 来触发 refresh token 流程
            // 这会在后端更新用户的 claims
            var refreshTokenResult = await _accessTokenProvider.RequestAccessToken(new AccessTokenRequestOptions
            {
                Scopes = new List<string> { "openid", "profile", "offline_access" }
            });

            if (refreshTokenResult.TryGetToken(out var newToken))
            {
                _logger.LogInformation("Token 刷新成功");
                return newToken.Value;
            }

            _logger.LogWarning("Token 刷新失败");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "强制刷新 token 时发生错误");
            return null;
        }
    }

    /// <summary>
    /// 在 WASM 模式下设置团队后，通过 refresh token 获取最新的身份验证状态
    /// </summary>
    public async Task SetCurrentTeamAsync(Guid teamId)
    {
        try
        {
            _logger.LogInformation("开始切换团队，团队ID: {TeamId}", teamId);

            // 短暂延迟确保后端团队信息更新完成
            await Task.Delay(100);

            // 强制刷新 token，获取最新的 claims
            var newToken = await ForceRefreshTokenAsync();

            _navigationManager.NavigateTo(_navigationManager.Uri, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换团队时发生错误，团队ID: {TeamId}", teamId);
        }
    }
}