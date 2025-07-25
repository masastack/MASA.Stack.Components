namespace Masa.Stack.Components.Infrastructure.Identity;

/// <summary>
/// Server 模式的团队状态管理器
/// 使用 AuthenticationStateManager 来管理团队状态
/// </summary>
public class ServerTeamStateManager : ITeamStateManager, IScopedDependency
{
    private readonly AuthenticationStateManager _authenticationStateManager;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public ServerTeamStateManager(
        AuthenticationStateManager authenticationStateManager,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateManager = authenticationStateManager;
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <summary>
    /// 通过更新身份验证状态中的声明来设置当前团队ID
    /// </summary>
    public async Task SetCurrentTeamAsync(Guid teamId)
    {
        await _authenticationStateManager.UpsertClaimAsync(IdentityClaimConsts.CURRENT_TEAM, teamId.ToString());
    }
}