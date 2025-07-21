namespace Masa.Stack.Components.Infrastructure.Identity;

/// <summary>
/// WASM 模式的团队状态管理器
/// 在 WASM 模式下，通过页面刷新获取包含最新团队信息的 token
/// 经测试发现 RequestAccessToken() 只返回缓存的 token，不会真正刷新，所以使用页面刷新确保可靠性
/// </summary>
public class WasmTeamStateManager : ITeamStateManager, IScopedDependency
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly NavigationManager _navigationManager;

    public WasmTeamStateManager(
        AuthenticationStateProvider authenticationStateProvider,
        NavigationManager navigationManager)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _navigationManager = navigationManager;
    }

        /// <summary>
    /// 在 WASM 模式下设置团队后，获取最新的身份验证状态
    /// </summary>
    public async Task SetCurrentTeamAsync(Guid teamId)
    {
        try
        {
            // 在 WASM 模式下，最可靠的方式是直接刷新页面
            // 因为 RequestAccessToken() 通常只返回缓存的 token，不会真正刷新
            // 而团队信息的更新需要服务端重新颁发包含最新 claims 的 token
            
            // 短暂延迟确保后端团队信息更新完成
            await Task.Delay(100);
            
            // 直接使用页面刷新，这是最可靠的方式
            // 页面刷新会重新初始化 OIDC 客户端，获取最新的 token 和 claims
            _navigationManager.NavigateTo(_navigationManager.Uri, true);
        }
        catch (Exception)
        {
            // 确保在任何异常情况下都能刷新页面
            _navigationManager.NavigateTo(_navigationManager.Uri, true);
        }
    }

    /// <summary>
    /// 从当前身份验证状态中获取团队ID
    /// </summary>
    public async Task<Guid> GetCurrentTeamAsync()
    {
        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var teamIdClaim = authState.User.FindFirst(IdentityClaimConsts.CURRENT_TEAM);

            if (teamIdClaim != null && Guid.TryParse(teamIdClaim.Value, out var teamId))
            {
                return teamId;
            }
        }
        catch (Exception)
        {
            // 如果获取失败，返回空的 GUID
        }

        return Guid.Empty;
    }

    /// <summary>
    /// 清除团队状态
    /// </summary>
    public async Task ClearTeamStateAsync()
    {
        await SetCurrentTeamAsync(Guid.Empty);
    }
}