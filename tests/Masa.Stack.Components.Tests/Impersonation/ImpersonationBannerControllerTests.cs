using Masa.Stack.Components.Infrastructure.Impersonation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Xunit;
using static Masa.Stack.Components.Tests.Impersonation.ImpersonationTestData;

namespace Masa.Stack.Components.Tests.Impersonation;

/// <summary>
/// 横幅状态机（<see cref="Masa.Stack.Components.Layouts.ImpersonationBannerController"/>）的全部流转：
/// 初始化/激活、倒计时、自然过期、主动结束、跨 tab 同步（结束/换人/同 token）、外部联动与释放。
/// 派发是内联加锁的（模拟组件的 InvokeAsync 语义），倒计时由测试直接调用 TickAsync 驱动。
/// </summary>
public class ImpersonationBannerControllerTests
{
    // ------------------------------------------------------------ 初始化与激活

    [Fact]
    public async Task Initialize_WithAnActiveSession_BecomesVisible_StartsTheCountdown_AndSubscribesStorage()
    {
        var h = new BannerHarness();
        await h.CaptureAndInitializeAsync();

        Assert.True(h.Controller.Visible);
        Assert.InRange(h.Controller.Remaining.TotalSeconds, 0, 601);
        Assert.Empty(h.Ui.WarningsSnapshot());
        Assert.Equal(0, h.Ui.Reloads);
        // 已订阅跨 tab storage 事件（且只订阅一次）
        Assert.Single(h.SessionHarness.Module.ActiveSubscriptions);
    }

    [Fact]
    public async Task Initialize_WithoutASession_StaysHidden_AndTicksDoNothing()
    {
        var h = new BannerHarness();
        await h.Controller.InitializeAsync();

        Assert.False(h.Controller.Visible);
        Assert.Equal(0, h.Controller.Remaining.TotalSeconds);

        await h.Controller.TickAsync();

        Assert.Equal(0, h.Ui.Reloads);
        Assert.Empty(h.Ui.WarningsSnapshot());
    }

    [Fact]
    public async Task Initialize_WithAnExpiredStoredSession_StaysHidden_WithoutAnyUserFeedback()
    {
        // 横幅从未显示过：过期会话在加载时被静默清理，不应弹提示或刷新
        var h = new BannerHarness();
        StoreSession(h.SessionHarness.Module, CreateToken(), DateTimeOffset.UtcNow.AddSeconds(-1));

        await h.Controller.InitializeAsync();

        Assert.False(h.Controller.Visible);
        Assert.False(h.Session.IsActive);
        Assert.Empty(h.SessionHarness.Module.Storage);
        Assert.Equal(0, h.Ui.Reloads);
        Assert.Empty(h.Ui.WarningsSnapshot());
    }

    [Fact]
    public async Task SessionActivated_AfterInitialize_StartsTheBanner_AndNotifies()
    {
        // 初始化时没有模拟 → 后续捕获（如另一处触发的 Changed）激活横幅。
        // 状态机内的 StartTimer 会启动真实 Timer（首个 tick 在线程池上派发），所以这里只断言「至少通知一次」
        var h = new BannerHarness();
        await h.Controller.InitializeAsync();
        var notified = 0;
        h.Controller.StateChanged += () => System.Threading.Interlocked.Increment(ref notified);

        await h.CaptureAsync();

        Assert.True(h.Controller.Visible);
        Assert.True(notified >= 1);
    }

    // ------------------------------------------------------------ 发起人显示名（三层回退）

    [Fact]
    public async Task ActorName_ComesFromTheActClaim_WhenTheSenderProvidesIt()
    {
        var h = new BannerHarness();
        h.Inner.User = FakeInnerProvider.PersonWithId(ActorId, "Alice");

        await h.CaptureAndInitializeAsync(new TokenSpec { ActUserName = "Operator One" });

        // act.userName 优先，即使本人身份也能对上也用 token 里自带的
        Assert.Equal("Operator One", h.Controller.ActorDisplayName);
    }

    [Fact]
    public async Task ActorName_FallsBackToTheOperatorsOwnIdentity_WhenTheActClaimOmitsIt()
    {
        var h = new BannerHarness();
        h.Inner.User = FakeInnerProvider.PersonWithId(ActorId, "Alice");

        await h.CaptureAndInitializeAsync();

        Assert.Equal("Alice", h.Controller.ActorDisplayName);
    }

    [Fact]
    public async Task ActorName_FallsBackToTheRawId_WhenSomebodyElseIsLoggedIn()
    {
        // 链接被转发到另一人的浏览器：act.sub 与当前登录者不一致，绝不能冒用其姓名
        var h = new BannerHarness();
        h.Inner.User = FakeInnerProvider.PersonWithId("99999999-9999-9999-9999-999999999999", "Mallory");

        await h.CaptureAndInitializeAsync();

        Assert.Equal(ActorId, h.Controller.ActorDisplayName);
    }

    [Fact]
    public async Task ActorName_FallsBackToTheRawId_WhenNoAuthenticationStateProviderIsRegistered()
    {
        // 未接入 OIDC 的宿主：无法比对本人身份，兜底显示 Guid
        var h = new BannerHarness(registerDecorator: false);

        await h.CaptureAndInitializeAsync();

        Assert.Equal(ActorId, h.Controller.ActorDisplayName);
    }

    [Fact]
    public async Task ActorName_FallsBackToTheRawId_WhenTheProviderIsNotTheDecorator()
    {
        // 自建认证装配（provider 不是 ImpersonatedAuthenticationStateProvider）
        var custom = new FakeInnerProvider { User = FakeInnerProvider.PersonWithId(ActorId, "Alice") };
        var h = new BannerHarness(provider: custom, registerDecorator: false);

        await h.CaptureAndInitializeAsync();

        Assert.Equal(ActorId, h.Controller.ActorDisplayName);
    }

    // ------------------------------------------------------------ 倒计时

    [Fact]
    public async Task Tick_RefreshesTheRemainingTime_WithoutAnyUserFeedback()
    {
        var h = new BannerHarness();
        await h.CaptureAndInitializeAsync();
        await Task.Delay(50);

        await h.Controller.TickAsync();

        Assert.True(h.Controller.Visible);
        Assert.True(h.Controller.Remaining > TimeSpan.Zero);
        Assert.InRange(h.Controller.Remaining.TotalSeconds, 0, 600);
        Assert.Empty(h.Ui.WarningsSnapshot());
        Assert.Equal(0, h.Ui.Reloads);
    }

    // ------------------------------------------------------------ 自然过期（走真实时钟，tick 由测试驱动）

    [Fact]
    public async Task NaturalExpiry_WarnsExpired_EndsTheSession_AndReloads()
    {
        var h = new BannerHarness();
        await h.CaptureAndInitializeAsync(new TokenSpec { Expires = DateTime.UtcNow.AddSeconds(1) });
        Assert.True(h.Controller.Visible);

        // 真实时钟推进到过期；每个 tick 都由测试线程直接驱动
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (h.Session.IsActive && DateTime.UtcNow < deadline)
        {
            await h.Controller.TickAsync();
            await Task.Delay(20);
        }
        await h.Controller.TickAsync();

        Assert.False(h.Controller.Visible);
        Assert.False(h.Session.IsActive);
        Assert.Empty(h.SessionHarness.Module.Storage); // 会话被清除，localStorage 不残留
        Assert.Equal(1, h.Ui.Reloads);
        Assert.Equal(new[] { "Impersonation.Banner.Expired" }, h.Ui.WarningsSnapshot());
    }

    // ------------------------------------------------------------ 主动结束（横幅按钮）

    [Fact]
    public async Task EndAsync_ClearsTheSession_Reloads_WithoutAnySnackbar_AndPublishesOwnIdentity()
    {
        var h = new BannerHarness();
        h.Inner.User = FakeInnerProvider.PersonWithId(ActorId, "Alice");
        await h.CaptureAndInitializeAsync();

        var received = new TaskCompletionSource<Task<AuthenticationState>>(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Decorator!.AuthenticationStateChanged += task => received.TrySetResult(task);

        await h.Controller.EndAsync();

        // 会话清除 + 整页刷新，但用户主动结束不需要提示
        Assert.False(h.Session.IsActive);
        Assert.Empty(h.SessionHarness.Module.Storage);
        Assert.Equal(1, h.Ui.Reloads);
        Assert.Empty(h.Ui.WarningsSnapshot());

        // 走的是装饰器路径：身份变化被推给订阅者（UI 立即回落本人）
        var state = await await received.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal("Alice", state.User.Identity!.Name);
    }

    [Fact]
    public async Task EndAsync_RunsOnlyOnce()
    {
        var h = new BannerHarness();
        await h.CaptureAndInitializeAsync();

        await h.Controller.EndAsync();
        await h.Controller.EndAsync();

        Assert.Equal(1, h.Ui.Reloads);
    }

    [Fact]
    public async Task EndAsync_FallsBackToTheSession_WhenNoDecoratorIsRegistered()
    {
        var h = new BannerHarness(registerDecorator: false);
        await h.CaptureAndInitializeAsync();

        await h.Controller.EndAsync();

        Assert.False(h.Session.IsActive);
        Assert.Empty(h.SessionHarness.Module.Storage);
        Assert.Equal(1, h.Ui.Reloads);
    }

    // ------------------------------------------------------------ 跨 tab：storage 事件

    [Fact]
    public async Task StorageChanged_OtherTabEnded_WarnsEndedElsewhere_AndFallsBack()
    {
        var h = new BannerHarness();
        await h.CaptureAndInitializeAsync();
        h.SessionHarness.Module.Storage.Clear(); // 其它 tab 结束了模拟

        await h.Controller.HandleStorageChangedAsync();

        Assert.False(h.Controller.Visible);
        Assert.False(h.Session.IsActive);
        Assert.Equal(1, h.Ui.Reloads);
        Assert.Equal(new[] { "Impersonation.Banner.EndedElsewhere" }, h.Ui.WarningsSnapshot());
    }

    [Fact]
    public async Task StorageChanged_ExpiredElsewhere_WarnsExpired_NotEndedElsewhere()
    {
        // 其它 tab 交付的 token 到期：文案必须是「已过期」，不能误报成「其它 tab 结束」
        var h = new BannerHarness();
        await h.CaptureAndInitializeAsync();
        StoreSession(h.SessionHarness.Module, CreateToken(), DateTimeOffset.UtcNow.AddSeconds(-1));

        await h.Controller.HandleStorageChangedAsync();

        Assert.False(h.Controller.Visible);
        Assert.Equal(new[] { "Impersonation.Banner.Expired" }, h.Ui.WarningsSnapshot());
        Assert.Equal(1, h.Ui.Reloads);
    }

    [Fact]
    public async Task StorageChanged_OtherTabSwitchedTarget_WarnsChangedElsewhere_ReloadsButKeepsTheSession()
    {
        var h = new BannerHarness();
        await h.CaptureAndInitializeAsync();

        var other = "33333333-3333-3333-3333-333333333333";
        var otherToken = CreateToken(new TokenSpec { Sub = other });
        StoreSession(h.SessionHarness.Module, otherToken, DateTimeOffset.UtcNow.AddMinutes(5), targetUserId: other);

        await h.Controller.HandleStorageChangedAsync();

        // 换人：提示 + 整页刷新，但会话保持（仍然有效，刷新后按新身份初始化）
        Assert.True(h.Controller.Visible);
        Assert.True(h.Session.IsActive);
        Assert.Equal(otherToken, h.Session.AccessToken);
        Assert.Equal(new[] { "Impersonation.Banner.ChangedElsewhere" }, h.Ui.WarningsSnapshot());
        Assert.Equal(1, h.Ui.Reloads);
    }

    [Fact]
    public async Task StorageChanged_SameToken_OnlyRefreshesTheCountdown()
    {
        var h = new BannerHarness();
        await h.CaptureAndInitializeAsync();

        await h.Controller.HandleStorageChangedAsync();

        Assert.True(h.Controller.Visible);
        Assert.Empty(h.Ui.WarningsSnapshot());
        Assert.Equal(0, h.Ui.Reloads);
    }

    [Fact]
    public async Task StorageChanged_IsIgnoredOnceEnding()
    {
        var h = new BannerHarness();
        await h.CaptureAndInitializeAsync();
        await h.Controller.EndAsync(); // _ending = true，刷新将发未发

        h.SessionHarness.Module.Storage.Clear();
        await h.Controller.HandleStorageChangedAsync();

        // 不追加提示、不重复刷新
        Assert.Equal(1, h.Ui.Reloads);
        Assert.Empty(h.Ui.WarningsSnapshot());
    }

    // ------------------------------------------------------------ 会话事件联动与释放

    [Fact]
    public async Task SessionEndedExternally_HidesTheBanner_WithoutFallingBack()
    {
        // 装饰器/其它流程结束了会话：横幅静默跟随隐藏（身份已由装饰器推送），无需提示或刷新
        var h = new BannerHarness();
        await h.CaptureAndInitializeAsync();

        await h.Session.EndAsync();

        Assert.False(h.Controller.Visible);
        Assert.Empty(h.Ui.WarningsSnapshot());
        Assert.Equal(0, h.Ui.Reloads);

        // 隐藏后再 tick：无副作用
        await h.Controller.TickAsync();
        Assert.Equal(0, h.Ui.Reloads);
    }

    [Fact]
    public async Task Dispose_UnsubscribesFromTheSessionAndFromStorage_FurtherChangesAreIgnored()
    {
        var h = new BannerHarness();
        await h.CaptureAndInitializeAsync();
        var notified = 0;
        h.Controller.StateChanged += () => System.Threading.Interlocked.Increment(ref notified);

        await h.Controller.DisposeAsync();

        Assert.Empty(h.SessionHarness.Module.ActiveSubscriptions);

        // 会话再怎么变化，死掉的横幅不再响应
        await h.Session.EndAsync();
        await Task.Delay(30);
        Assert.Equal(0, notified);

        // 幂等：重复释放不抛
        await h.Controller.DisposeAsync();
    }
}
