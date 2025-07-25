namespace Masa.Stack.Components.Infrastructure.Identity;

/// <summary>
/// 团队状态管理接口，用于兼容 Server 和 WASM 模式
/// </summary>
public interface ITeamStateManager
{
    /// <summary>
    /// 设置当前团队ID
    /// </summary>
    /// <param name="teamId">团队ID</param>
    /// <returns></returns>
    Task SetCurrentTeamAsync(Guid teamId);
}