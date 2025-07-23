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

    /// <summary>
    /// 获取当前团队ID
    /// </summary>
    /// <returns>团队ID，如果没有设置则返回 Guid.Empty</returns>
    Task<Guid> GetCurrentTeamAsync();

    /// <summary>
    /// 清除团队状态
    /// </summary>
    /// <returns></returns>
    Task ClearTeamStateAsync();
} 