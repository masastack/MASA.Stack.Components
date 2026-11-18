// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

using Masa.Stack.Components.Infrastructure.Impersonation;

namespace Masa.Stack.Components.Layouts;

/// <summary>
/// 模拟登录横幅：显示"正在模拟 [目标用户] · 由 [操作者] 发起 · 剩余时间"，提供"结束模拟"。
/// 退出闭环：主动结束（通知状态并整页刷新回落本人）、自然过期（倒计时归零自动回落）、
/// 登出联动（AuthenticationStateProvider 装饰器在本人登出时清除模拟态）、
/// 多 tab 同步（订阅 localStorage 的 <c>storage</c> 事件，其它 tab 结束则本 tab 跟随回落）。
/// 状态机在 <see cref="ImpersonationBannerController"/>（可单元测试），本类只做渲染与接线。
/// </summary>
public partial class ImpersonationBanner
{
    [Inject]
    private ImpersonationSession ImpersonationSession { get; set; } = default!;

    /// <summary>
    /// 不能用 [Inject] AuthenticationStateProvider：未接入 OIDC 的宿主（只用 SLayout、自行装配认证）
    /// 并未注册该服务，直接注入会让布局渲染失败。这里按需解析，缺失时降级为只清本地会话。
    /// </summary>
    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ImpersonationBannerController? _controller;
    private DotNetObjectReference<ImpersonationBanner>? _selfRef;

    /// <summary>
    /// 倒计时文案（"剩余 mm:ss"）。
    /// 注意：<c>T("key", "09:30")</c> 传两个 string 时，C# 会选中 <c>T(string scope, string key, ...)</c> 重载，
    /// 把 "09:30" 当成 key 去查，结果只剩 "09:30"，丢掉"剩余/Expires in"前缀。
    /// 因此这里显式传 <c>object[]</c>，只可能匹配 <c>T(string key, params object[] args)</c>。
    /// </summary>
    private string RemainingLabel
    {
        get
        {
            var remaining = _controller?.Remaining ?? TimeSpan.Zero;
            var clock = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
            return T("Impersonation.Banner.Remaining", new object[] { clock });
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _controller = new ImpersonationBannerController(
            ImpersonationSession,
            () => Services.GetService(typeof(AuthenticationStateProvider)) as AuthenticationStateProvider,
            T,
            message => PopupService.EnqueueSnackbarAsync(message, AlertTypes.Warning),
            () =>
            {
                // 用 NavigationManager 强制刷新，不依赖自定义 JS 全局函数（缺失时旧实现会静默失败、停在旧身份上）。
                // 必须刷新 OriginalNavigationManager：MicroFrontendNavigationManager 只在构造时 Initialize 一次 Uri，
                // 站内跳转后它的基类 Uri 已过期，Refresh 会跳回「构造时」的地址（可能是已失效的登录回调 URL），而不是当前页。
                NavigationManager.OriginalNavigationManager.Refresh(forceReload: true);
                return Task.CompletedTask;
            },
            operation => InvokeAsync(operation),
            millisecondsDelay => Task.Delay(millisecondsDelay),
            () => _selfRef ??= DotNetObjectReference.Create(this),
            Logger);
        _controller.StateChanged += OnControllerStateChanged;

        await _controller.InitializeAsync();
    }

    private void OnControllerStateChanged() => _ = InvokeAsync(StateHasChanged);

    /// <summary><c>storage</c> 事件回调（由 JS 桥经 DotNetObjectReference 回调）：转发给状态机。</summary>
    [JSInvokable]
    public Task OnStorageChanged() => _controller?.HandleStorageChangedAsync() ?? Task.CompletedTask;

    /// <summary>横幅"结束模拟"按钮。</summary>
    private Task EndAsync() => _controller?.EndAsync() ?? Task.CompletedTask;

    protected override async ValueTask DisposeAsyncCore()
    {
        // 基类（DisposableComponentBase）只实现 IAsyncDisposable，Blazor 只会走 DisposeAsync，
        // 因此清理必须放在 DisposeAsyncCore 里；写一个 public Dispose() 是死代码。
        // 这里可以真正 await：JS 退订是异步的，写在同步 Dispose() 里会丢。
        if (_controller is not null)
        {
            _controller.StateChanged -= OnControllerStateChanged;
            await _controller.DisposeAsync();
        }

        // 先退订（控制器），再释放回调对象；顺序颠倒会让 JS 侧对已释放对象发起回调
        _selfRef?.Dispose();
        _selfRef = null;

        await base.DisposeAsyncCore();
    }
}
