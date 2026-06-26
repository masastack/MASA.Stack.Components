namespace Masa.Stack.Components.Infrastructure;

/// <summary>
/// 由 <see cref="SLayout"/> 维护的全局 Sapp 导航配置（当前作用域内有效）。
/// </summary>
public class SappNavigationContext : IScopedDependency
{
    public bool UseSappNav { get; private set; }

    public string? AppIdOverride { get; private set; }

    public void Update(bool useSappNav, string? appIdOverride = null)
    {
        UseSappNav = useSappNav;
        AppIdOverride = appIdOverride.IsNullOrEmpty() ? null : appIdOverride;
    }
}
