// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

using System.Security.Claims;
using Masa.Stack.Components.Infrastructure.Impersonation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace Masa.Stack.Components.Layouts;

/// <summary>
/// 模拟登录横幅的状态机：倒计时、跨 tab 同步、过期/远端结束/换人三种回落路径。
/// 从 <see cref="ImpersonationBanner"/>（薄壳组件）中抽出，组件只负责渲染与接线，
/// 本类不依赖 Blazor 渲染器，全部状态流转可被单元测试直接驱动。
/// <para>
/// 与组件的协作契约：
///  - 组件把 <c>InvokeAsync</c> 作为 <paramref name="dispatch"/> 传入，保证所有状态变更都发生在渲染器同步上下文；
///  - 状态变化通过 <see cref="StateChanged"/> 通知组件触发 <c>StateHasChanged</c>；
///  - <see cref="HandleStorageChangedAsync"/> 由组件的 <c>[JSInvokable]</c> 方法转发（storage 事件的跨 tab 通知）。
/// </para>
/// </summary>
internal sealed class ImpersonationBannerController : IAsyncDisposable
{
    private const string ExpiredKey = "Impersonation.Banner.Expired";
    private const string EndedElsewhereKey = "Impersonation.Banner.EndedElsewhere";
    private const string ChangedElsewhereKey = "Impersonation.Banner.ChangedElsewhere";

    private readonly ImpersonationSession _session;
    private readonly Func<AuthenticationStateProvider?> _authenticationStateProviderResolver;
    private readonly Func<string, string> _translate;
    private readonly Func<string, Task> _showWarning;
    private readonly Func<Task> _reload;
    private readonly Func<Func<Task>, Task> _dispatch;
    private readonly Func<int, Task> _delay;
    private readonly Func<object> _storageCallbackReceiverFactory;
    private readonly ILogger _logger;

    private Timer? _timer;

    /// <summary>storage 订阅 id（订阅失败为 null）。退订时只摘自己的，不影响同一文档里其它订阅者。</summary>
    private int? _subscriptionId;

    private bool _visible;
    private bool _ending;
    private TimeSpan _remaining;
    private string _actorDisplayName = string.Empty;

    /// <summary>倒计时周期。测试可以调短，或调长后直接调用 <see cref="TickAsync"/> 驱动。</summary>
    internal TimeSpan TickInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>倒计时只在本地走表（不访问 JS），跨 tab 变更由 storage 事件推送。</summary>
    public TimeSpan Remaining => _remaining < TimeSpan.Zero ? TimeSpan.Zero : _remaining;
    public bool Visible => _visible;

    /// <summary>
    /// 「发起人」的显示名。解析顺序（在 <see cref="ResolveActorDisplayNameAsync"/> 里落地）：
    /// 1) act claim 自带 userName（RFC 8693 附加属性，发送端可选提供）；
    /// 2) 本人（内层 OIDC）身份与 act.sub 是同一人时，取本人的姓名；
    /// 3) 兜底为操作者 Guid。
    /// </summary>
    public string ActorDisplayName => string.IsNullOrEmpty(_actorDisplayName) ? _session.ActorUserId : _actorDisplayName;

    /// <summary>状态变化（可见性/倒计时/发起人），组件订阅后触发 StateHasChanged。</summary>
    public event Action? StateChanged;

    public ImpersonationBannerController(
        ImpersonationSession session,
        Func<AuthenticationStateProvider?> authenticationStateProviderResolver,
        Func<string, string> translate,
        Func<string, Task> showWarning,
        Func<Task> reload,
        Func<Func<Task>, Task> dispatch,
        Func<int, Task> delay,
        Func<object> storageCallbackReceiverFactory,
        ILogger logger)
    {
        _session = session;
        _authenticationStateProviderResolver = authenticationStateProviderResolver;
        _translate = translate;
        _showWarning = showWarning;
        _reload = reload;
        _dispatch = dispatch;
        _delay = delay;
        _storageCallbackReceiverFactory = storageCallbackReceiverFactory;
        _logger = logger;
    }

    /// <summary>组件 OnInitializedAsync 调用：加载会话、订阅变更，激活时启动倒计时。</summary>
    public async Task InitializeAsync()
    {
        await _session.EnsureLoadedAsync();
        _session.Changed += OnSessionChanged;
        _visible = _session.IsActive;
        _remaining = _visible ? _session.ExpiresAt - DateTimeOffset.Now : TimeSpan.Zero;
        if (_visible)
        {
            await StartAsync();
        }
    }

    /// <summary>
    /// 会话状态变化（建立/结束/跨 tab 同步）时重新裁决可见性。
    /// 已进入回落流程（<c>_ending</c>）时不能再重启定时器，否则会和刚停掉的定时器抢着触发回落。
    /// </summary>
    private void OnSessionChanged() => _ = _dispatch(async () =>
    {
        _visible = _session.IsActive;
        if (_visible)
        {
            if (!_ending)
            {
                // 激活/换新时同步倒计时初值，显示不依赖首个 tick 的到来
                _remaining = _session.ExpiresAt - DateTimeOffset.Now;
                await StartAsync();
            }
        }
        else
        {
            StopTimer();
        }

        StateChanged?.Invoke();
    });

    /// <summary>启动倒计时、解析发起人显示名并订阅跨 tab 变更。</summary>
    private async Task StartAsync()
    {
        StartTimer();
        await ResolveActorDisplayNameAsync();

        if (_subscriptionId is not null)
        {
            return;
        }

        // 回调对象由组件持有生命周期（DotNetObjectReference），Dispose 时与之配对退订。
        var receiver = _storageCallbackReceiverFactory();
        _subscriptionId = await _session.SubscribeStorageAsync(receiver);
    }

    private void StartTimer()
    {
        if (_timer is not null)
        {
            return;
        }

        // 首个 tick 延迟一个周期：倒计时初值由激活路径就地写入，无需立即补一拍
        //（这也让「启动即触发」的线程池 tick 不存在，状态机的时序完全可测）。
        var period = (int)TickInterval.TotalMilliseconds;
        _timer = new Timer(async _ => await TickAsync(), null, period, period);
    }

    /// <summary>
    /// 每秒倒计时。供定时器与测试共同驱动。
    /// <para>
    /// 过期要区分两种文案：token 仍在（IsExpired）说明是「自然到期」；token 已被清空说明是「其它 tab 结束」。
    /// （原先只看 IsActive，自然过期会误报 EndedElsewhere，而 Expired 分支反而是死代码。）
    /// </para>
    /// </summary>
    public async Task TickAsync()
    {
        try
        {
            await _dispatch(async () =>
            {
                if (_timer is null || _ending)
                {
                    return;
                }

                if (!_session.IsActive)
                {
                    if (_visible)
                    {
                        await FallBackAsync(ResolveEndReasonKey());
                    }
                    return;
                }

                _remaining = _session.ExpiresAt - DateTimeOffset.Now;
                if (_remaining <= TimeSpan.Zero)
                {
                    // 防御：IsActive 与 _remaining 两次取 Now 之间存在极窄竞态，到期判定以这里为准
                    await FallBackAsync(ExpiredKey);
                    return;
                }

                StateChanged?.Invoke();
            });
        }
        catch (ObjectDisposedException)
        {
            // circuit 关闭，正常
        }
        catch (Exception ex)
        {
            // 定时器回调是 async void：任何异常逃逸都会成为未处理异常（WASM 下可能白屏）
            _logger.LogError(ex, "Impersonation banner tick failed");
        }
    }

    /// <summary>
    /// storage 事件回调：其它 tab 结束了模拟、把模拟对象换成了另一个人，或其交付的 token 已过期。
    /// </summary>
    public async Task HandleStorageChangedAsync()
    {
        await _dispatch(async () =>
        {
            if (_ending)
            {
                return;
            }

            var previousToken = _session.AccessToken;
            await _session.RefreshFromStorageAsync();
            _visible = _session.IsActive;

            if (!_visible)
            {
                // 会话读取时「token 仍在但已过期」的判定要在 Changed 链里被装饰器清掉之前做出，
                // 这里消费 RefreshFromStorageAsync 提前记下的事实（LastRefreshedSessionWasExpired）
                await FallBackAsync(_session.LastRefreshedSessionWasExpired ? ExpiredKey : EndedElsewhereKey);
                return;
            }

            if (!string.Equals(previousToken, _session.AccessToken, StringComparison.Ordinal))
            {
                // 其它 tab 改成模拟另一个用户：共享会话已换成新 token（API 已用新身份），
                // 但本 tab 的菜单/权限/用户上下文都是按旧身份加载的，只推送身份通知不够，整页刷新才一致。
                await ReloadForIdentityChangeAsync(ChangedElsewhereKey);
                return;
            }

            _remaining = _session.ExpiresAt - DateTimeOffset.Now;
            await StartAsync();
            StateChanged?.Invoke();
        });
    }

    /// <summary>
    /// 会话已不可用时区分「自然过期」与「被其它 tab 结束」。
    /// </summary>
    private string ResolveEndReasonKey()
        // IsExpired = token 仍在且已过期 → 自然过期；token 已被清空 → 其它 tab 结束（或装饰器联动清除）
        => _session.IsExpired ? ExpiredKey : EndedElsewhereKey;

    /// <summary>横幅"结束模拟"按钮：直接回落本人，不需要提示。</summary>
    public async Task EndAsync()
    {
        if (_ending)
        {
            return;
        }
        _ending = true;
        StopTimer();
        await EndSessionAsync();
        await ReloadAsync();
    }

    /// <summary>过期/远端结束：提示后整页刷新回落本人身份。</summary>
    private async Task FallBackAsync(string messageKey)
    {
        StopTimer();
        _ending = true;
        await EndSessionAsync();
        _visible = false;
        StateChanged?.Invoke();
        await NotifyAndReloadAsync(messageKey);
    }

    /// <summary>模拟对象被其它 tab 换成了另一个人：保留会话（仍然有效），提示后整页刷新以按新身份重新初始化。</summary>
    private async Task ReloadForIdentityChangeAsync(string messageKey)
    {
        StopTimer();
        _ending = true;
        await NotifyAndReloadAsync(messageKey);
    }

    private async Task NotifyAndReloadAsync(string messageKey)
    {
        try
        {
            await _showWarning(_translate(messageKey));
        }
        catch (Exception)
        {
            // 提示失败不阻断刷新
        }

        await _delay(1500);
        await ReloadAsync();
    }

    /// <summary>
    /// 结束模拟并立即把新的身份推给订阅者（CascadingAuthenticationState / MasaUser / AuthorizeView）。
    /// 光清 localStorage 不够：身份缓存仍在，会出现"页面显示被模拟者、请求已用本人 token"的半混合态。
    /// 接入 OIDC 的宿主走装饰器（联动推送身份变化）；未接入的宿主降级为只清本地会话。
    /// </summary>
    private async Task EndSessionAsync()
    {
        if (_authenticationStateProviderResolver() is ImpersonatedAuthenticationStateProvider impersonated)
        {
            await impersonated.EndImpersonationAsync();
        }
        else
        {
            await _session.EndAsync();
        }
    }

    private async Task ReloadAsync()
    {
        try
        {
            await _reload();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reload after ending impersonation");
        }
    }

    /// <summary>
    /// 解析「发起人」显示名。见 <see cref="ActorDisplayName"/> 的三层回退说明。
    /// 解析失败（无 provider / 本人身份不可得 / 不是同一人）时保持为空，属性兜底显示 Guid。
    /// </summary>
    private async Task ResolveActorDisplayNameAsync()
    {
        if (!_session.IsActive)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_session.ActorUserName))
        {
            _actorDisplayName = _session.ActorUserName;
            return;
        }

        if (_authenticationStateProviderResolver() is ImpersonatedAuthenticationStateProvider decorator)
        {
            try
            {
                var own = await decorator.GetOwnAuthenticationStateAsync();
                var ownUserId = FirstClaim(own.User, IdentityClaimConsts.USER_ID, "sub", ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(ownUserId)
                    && string.Equals(ownUserId, _session.ActorUserId, StringComparison.OrdinalIgnoreCase))
                {
                    // act.sub 与当前登录的本人是同一人：直接展示本人的姓名
                    var ownUserName = FirstClaim(own.User, IdentityClaimConsts.USER_NAME, "userName", ClaimTypes.Name)
                        ?? own.User.Identity?.Name;
                    if (!string.IsNullOrEmpty(ownUserName))
                    {
                        _actorDisplayName = ownUserName;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to resolve the actor display name from the operator's own identity");
            }
        }

        _actorDisplayName = string.Empty;
    }

    /// <summary>按候选 claim 类型逐个取第一个非空值（本人 principal 的 claim 类型随 IdP 配置而异）。</summary>
    private static string? FirstClaim(ClaimsPrincipal user, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }
        return null;
    }

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public async ValueTask DisposeAsync()
    {
        _session.Changed -= OnSessionChanged;
        StopTimer();

        if (_subscriptionId is { } subscriptionId)
        {
            _subscriptionId = null;
            await _session.UnsubscribeStorageAsync(subscriptionId);
        }
    }
}
