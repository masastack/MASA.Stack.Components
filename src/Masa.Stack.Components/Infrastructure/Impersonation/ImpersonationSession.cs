// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Masa.Stack.Components.Extensions.OpenIdConnect;

namespace Masa.Stack.Components.Infrastructure.Impersonation;

/// <summary>
/// 模拟登录会话（接收端唯一事实源）。
/// 发送端（Auth 管理后台）以 URL fragment 交付 RFC 8693 token-exchange 换得的短命 access_token
/// （sub=被模拟用户、act=操作者、≤900s、无 refresh_token）。本会话将其持久化到 localStorage，
/// 供 IAccessTokenProvider 与 AuthenticationStateProvider 两层装饰同源取用，保证 API 身份与 UI 身份一致。
///
/// 持久化介质说明（安全权衡，勿随意更改）：
/// 模拟 token 是「被模拟用户」的**有效访问令牌**，落在 localStorage 意味着任何同源脚本（含 XSS）可读，
/// 且在浏览器会话之间留存。这里刻意保留 localStorage 而非 sessionStorage，原因是模拟需要跨 tab 生效
/// （发送端开新 tab 交付 token，操作者可能在多个 tab 里协作），sessionStorage 是 per-tab 的，语义不成立。
/// 风险由此收敛：token 由发送端按 ≤900s 短命签发、无 refresh_token、到期即由本会话主动清除，
/// 且结束模拟会立即 removeItem。若后续要进一步提升安全性，方向是改用内存态 + BroadcastChannel 同步，
/// 而不是换成 sessionStorage。
/// </summary>
public class ImpersonationSession : IAsyncDisposable
{
    public const string StorageKey = "masa.impersonation.session";

    private const string ModulePath = "./_content/Masa.Stack.Components/js/impersonation.js";

    /// <summary>IdentityServer4 默认的 userinfo 端点（相对 Authority）。</summary>
    private const string UserInfoPath = "/connect/userinfo";

    /// <summary>向 SSO 校验 token 的超时。超时按「无法验证」处理，不放行。</summary>
    private const int UserInfoTimeoutMilliseconds = 10_000;

    /// <summary>本地时钟允许落后 token 签发时间（nbf）的偏差。</summary>
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(5);

    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ImpersonationSession> _logger;
    private readonly MasaOpenIdConnectOptions? _oidcOptions;

    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private IJSObjectReference? _module;
    private bool _moduleUnavailable;

    private bool _loaded;

    public string AccessToken { get; private set; } = string.Empty;

    /// <summary>token 到期时间（本地时间，取签发 expires_in 与 JWT exp 的较早者）。</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    public string TargetUserId { get; private set; } = string.Empty;
    public string TargetUserName { get; private set; } = string.Empty;
    public string ActorUserId { get; private set; } = string.Empty;

    /// <summary>操作者（发起人）的显示名。来自 act claim 的 userName 附加属性（RFC 8693 允许）；
    /// 发送端未提供时为空，由横幅回退到「本人身份比对」或原始 Guid。</summary>
    public string ActorUserName { get; private set; } = string.Empty;

    /// <summary>
    /// 模拟 token 实际携带的 scope（来自 JWT 的 <c>scope</c>/<c>scp</c> claim）。
    /// 作为 <c>AccessToken.GrantedScopes</c> 如实返回给调用方，并用于在请求了未授予的 scope 时记录告警。
    /// </summary>
    public IReadOnlyList<string> GrantedScopes { get; private set; } = Array.Empty<string>();

    /// <summary>会话状态变化（建立/结束/跨 tab 同步）通知，横幅与状态装饰器订阅。</summary>
    public event Action? Changed;

    /// <summary>
    /// 最近一次 <see cref="RefreshFromStorageAsync"/> 读到的会话是否已过期（token 仍在但超过有效期）。
    /// 见该方法内的说明：过期事实必须在 Changed 事件链触发之前记录，供横幅判定过期文案。
    /// </summary>
    public bool LastRefreshedSessionWasExpired { get; private set; }

    public bool IsLoaded => _loaded;
    public bool IsExpired => !string.IsNullOrEmpty(AccessToken) && DateTimeOffset.Now >= ExpiresAt;
    public bool IsActive => _loaded && !string.IsNullOrEmpty(AccessToken) && !IsExpired;

    /// <param name="oidcOptions">
    /// 本应用的 OIDC 配置，用于校验模拟 token 的签发者并定位 userinfo 端点。
    /// 未注册（未接入 <c>AddMasaOpenIdConnectAsync</c> 的宿主）时为 null，此时拒绝捕获任何 token——
    /// 没有可信的 SSO 地址就无法验证，宁可不启用也不放行未验证的 token。
    /// </param>
    public ImpersonationSession(
        IJSRuntime jsRuntime,
        ILogger<ImpersonationSession> logger,
        MasaOpenIdConnectOptions? oidcOptions = null)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
        _oidcOptions = oidcOptions;
    }

    /// <summary>
    /// 应用启动（OIDC 初始化之前）调用：捕获 URL fragment 中的模拟 token，
    /// 从地址栏移除（history.replaceState），校验后写入 localStorage。
    /// 必须先于 <c>AddOidcAuthentication</c> 执行，登录重定向才不会丢失模拟态。
    /// <para>
    /// 校验分两层（任一失败都不落盘、不生效）：
    /// 1) 本地预检：iss 与 SSO 一致、sub/act.sub 是 Guid、带 act/exp/openid scope、nbf 不在未来、未过期；
    /// 2) 向 SSO 的 userinfo 端点出示 token——签名、有效期只有 SSO 能验（WASM 里没有可用的本地验签），
    ///    且返回的 sub 必须与 token 自称的目标用户一致。
    /// 这样 URL 里任何人都能拼出来的「伪造 token」无法把 UI 渲染成任意身份、也不会被当 Bearer 发给 API。
    /// </para>
    /// </summary>
    public async Task<bool> CaptureFromUrlAsync()
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return false;
        }

        CapturedFragment? fragment;
        try
        {
            fragment = await module.InvokeAsync<CapturedFragment?>("captureFragment");
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Failed to capture impersonation token from the URL fragment");
            return false;
        }

        if (fragment is null || string.IsNullOrWhiteSpace(fragment.AccessToken))
        {
            return false;
        }

        var authority = _oidcOptions?.Authority?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(authority))
        {
            _logger.LogWarning(
                "Rejected an impersonation token: no OIDC authority is configured, so the token cannot be verified");
            return false;
        }

        var expectedIssuer = string.IsNullOrWhiteSpace(_oidcOptions!.Issuer)
            ? authority
            : _oidcOptions.Issuer.Trim().TrimEnd('/');

        if (!TryParseToken(fragment.AccessToken, fragment.ExpiresIn, expectedIssuer, out var item, out var rejectReason))
        {
            _logger.LogWarning("Rejected an impersonation token: {Reason}", rejectReason);
            return false;
        }

        if (!await VerifyWithIdentityProviderAsync(module, authority, item))
        {
            return false;
        }

        if (!await WriteToStorageAsync(module, item))
        {
            return false;
        }

        Apply(item);
        _loaded = true;
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// 让 SSO 验证 token：GET {authority}/connect/userinfo，Bearer 出示该 token。
    /// SSO 会校验签名、有效期、client 与 scope；200 且返回的 <c>sub</c> 等于 token 自称的目标用户才算通过。
    /// 无法验证（网络/CORS 失败、超时、5xx）同样不放行（fail closed）：此时 URL 已被抹除，
    /// 操作者只需重新点一次「模拟登录」，而放行一个未验证的 token 的代价要大得多。
    /// </summary>
    private async Task<bool> VerifyWithIdentityProviderAsync(IJSObjectReference module, string authority, ImpersonationSessionItem item)
    {
        var endpoint = authority + UserInfoPath;

        UserInfoProbe? probe;
        try
        {
            probe = await module.InvokeAsync<UserInfoProbe?>(
                "probeUserInfo", endpoint, item.AccessToken, UserInfoTimeoutMilliseconds);
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Rejected an impersonation token: verification against {Endpoint} could not be performed", endpoint);
            return false;
        }

        if (probe is null || probe.Status == 0)
        {
            _logger.LogWarning(
                "Rejected an impersonation token: {Endpoint} is unreachable (network error, CORS or timeout), so it cannot be verified",
                endpoint);
            return false;
        }

        if (probe.Status != 200)
        {
            _logger.LogWarning(
                "Rejected an impersonation token: {Endpoint} answered HTTP {Status}. " +
                "The token is invalid or expired, or the identity provider does not accept it (it must carry the 'openid' scope)",
                endpoint, probe.Status);
            return false;
        }

        if (!string.Equals(probe.Sub, item.TargetUserId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Rejected an impersonation token: the identity provider reports a different subject than the token claims");
            return false;
        }

        return true;
    }

    /// <summary>惰性恢复：从 localStorage 读取会话（每个 scope 首次访问时调用一次）。恢复时已过期则直接清除。</summary>
    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        var item = await ReadFromStorageAsync();
        Apply(item);
        _loaded = true;

        if (!string.IsNullOrEmpty(AccessToken) && IsExpired)
        {
            await EndAsync();
        }
    }

    /// <summary>横幅定时器调用：重新读取 localStorage（多 tab 同步——其它 tab 结束模拟后本 tab 自动跟随）。</summary>
    public async Task RefreshFromStorageAsync()
    {
        var wasActive = IsActive;
        var previousToken = AccessToken;
        var item = await ReadFromStorageAsync();

        // 「读到的是过期 token」这一事实必须在 Changed 事件链里任何监听者（身份装饰器会在收到事件时
        // 立即清除过期会话）动手之前记录下来，横幅据此区分「自然过期」与「被其它 tab 结束」。
        LastRefreshedSessionWasExpired =
            item is { AccessToken.Length: > 0, ExpiresAtUnixSeconds: > 0 }
            && DateTimeOffset.Now >= DateTimeOffset.FromUnixTimeSeconds(item.ExpiresAtUnixSeconds);

        Apply(item);
        _loaded = true;

        // 除了「有/无」的切换，还要识别「换人」：其它 tab 直接改去模拟另一个用户时 IsActive 前后都是 true，
        // 但身份已经变了，订阅者（身份装饰器、横幅）必须收到通知。
        if (IsActive != wasActive || !string.Equals(previousToken, AccessToken, StringComparison.Ordinal))
        {
            Changed?.Invoke();
        }
    }

    /// <summary>结束模拟：清除持久化与内存态，本人（OIDC）会话不受影响。</summary>
    public async Task EndAsync()
    {
        var had = !string.IsNullOrEmpty(AccessToken);

        var module = await GetModuleAsync();
        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync("clearSession", StorageKey);
            }
            catch (JSDisconnectedException)
            {
            }
            catch (JSException ex)
            {
                _logger.LogWarning(ex, "Failed to clear the impersonation session from localStorage");
            }
        }

        AccessToken = string.Empty;
        ExpiresAt = default;
        TargetUserId = string.Empty;
        TargetUserName = string.Empty;
        ActorUserId = string.Empty;
        ActorUserName = string.Empty;
        GrantedScopes = Array.Empty<string>();
        _loaded = true;

        if (had)
        {
            Changed?.Invoke();
        }
    }

    /// <summary>取得（并缓存）JS 桥接模块。不可用时只告警一次，之后直接返回 null，不反复打 JS。</summary>
    private async Task<IJSObjectReference?> GetModuleAsync()
    {
        if (_module is not null)
        {
            return _module;
        }
        if (_moduleUnavailable)
        {
            return null;
        }

        await _moduleLock.WaitAsync();
        try
        {
            if (_module is not null)
            {
                return _module;
            }
            if (_moduleUnavailable)
            {
                return null;
            }

            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
            return _module;
        }
        catch (JSDisconnectedException)
        {
            _moduleUnavailable = true;
            return null;
        }
        catch (JSException ex)
        {
            _moduleUnavailable = true;
            // 这是「功能静默失效」的唯一可见信号：脚本缺失/加载失败时必须留下痕迹
            _logger.LogError(ex,
                "Impersonation JS bridge could not be loaded from {Path}; impersonation is disabled for this session",
                ModulePath);
            return null;
        }
        catch (InvalidOperationException ex)
        {
            // prerender / JS interop 不可用阶段
            _logger.LogDebug(ex, "JS interop is not available yet; impersonation capture deferred");
            return null;
        }
        finally
        {
            _moduleLock.Release();
        }
    }

    private async Task<ImpersonationSessionItem?> ReadFromStorageAsync()
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return null;
        }

        try
        {
            return await module.InvokeAsync<ImpersonationSessionItem?>("readSession", StorageKey);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Failed to read the impersonation session from localStorage");
        }
        return null;
    }

    /// <summary>
    /// 订阅跨 tab 变更（由 <c>storage</c> 事件驱动，不轮询）。
    /// 返回订阅 id，失败返回 null。每个订阅者持有自己的 id，互不顶替；
    /// 回调对象由调用方持有生命周期，<see cref="UnsubscribeStorageAsync"/> 必须与之配对。
    /// </summary>
    public async Task<int?> SubscribeStorageAsync(object callbackReceiver)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return null;
        }

        try
        {
            var id = await module.InvokeAsync<int>("subscribeStorage", StorageKey, callbackReceiver);
            return id > 0 ? id : null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Failed to subscribe to impersonation session storage events");
            return null;
        }
    }

    /// <summary>取消跨 tab 订阅（组件释放时调用）。只摘除 <paramref name="subscriptionId"/> 对应的那一个。</summary>
    public async Task UnsubscribeStorageAsync(int subscriptionId)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        try
        {
            await module.InvokeVoidAsync("unsubscribeStorage", subscriptionId);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException ex)
        {
            _logger.LogDebug(ex, "Failed to unsubscribe from impersonation session storage events");
        }
    }

    private async Task<bool> WriteToStorageAsync(IJSObjectReference module, ImpersonationSessionItem item)
    {
        try
        {
            await module.InvokeVoidAsync("writeSession", StorageKey, item);
            return true;
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Failed to persist the impersonation session to localStorage");
            return false;
        }
    }

    private void Apply(ImpersonationSessionItem? item)
    {
        AccessToken = item?.AccessToken ?? string.Empty;
        ExpiresAt = item is { ExpiresAtUnixSeconds: > 0 }
            ? DateTimeOffset.FromUnixTimeSeconds(item.ExpiresAtUnixSeconds).ToLocalTime()
            : default;
        TargetUserId = item?.TargetUserId ?? string.Empty;
        TargetUserName = item?.TargetUserName ?? string.Empty;
        ActorUserId = item?.ActorUserId ?? string.Empty;
        ActorUserName = item?.ActorUserName ?? string.Empty;
        GrantedScopes = item?.GrantedScopes is { Count: > 0 } scopes ? scopes : Array.Empty<string>();
    }

    /// <summary>
    /// 本地预检并解析模拟 token 的 claims（sub=目标用户、act.sub=操作者）。无效 token 拒绝落盘。
    /// 这里不验签（WASM 无法本地验签，由 <see cref="VerifyWithIdentityProviderAsync"/> 交给 SSO），
    /// 只做便宜、确定的结构性检查，把明显不是 SSO 模拟 token 的东西挡在网络请求之前。
    /// </summary>
    /// <param name="rejectReason">被拒绝时的原因（不含 token 内容），用于日志；通过时为 null。</param>
    private static bool TryParseToken(
        string accessToken,
        int expiresIn,
        string expectedIssuer,
        out ImpersonationSessionItem item,
        out string? rejectReason)
    {
        item = new ImpersonationSessionItem
        {
            AccessToken = accessToken,
            ExpiresAtUnixSeconds = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToUnixTimeSeconds(),
        };
        rejectReason = null;

        JwtSecurityToken jwt;
        try
        {
            jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        }
        catch (Exception)
        {
            rejectReason = "it is not a well-formed JWT";
            return false;
        }

        // iss 必须是本应用信任的那个 SSO（忽略尾部 '/' 与大小写）。
        // 实际签发者与 Authority 不同的部署用 MasaOpenIdConnectOptions.Issuer 显式指定。
        var issuer = (jwt.Issuer ?? string.Empty).Trim().TrimEnd('/');
        if (!string.Equals(issuer, expectedIssuer, StringComparison.OrdinalIgnoreCase))
        {
            rejectReason = $"issuer '{Truncate(issuer)}' does not match the expected '{expectedIssuer}' " +
                           "(set MasaOpenIdConnectOptions.Issuer if the identity provider signs with a different issuer)";
            return false;
        }

        // sub（被模拟用户）与 act.sub（操作者）都是 SSO 签发时的用户 Guid
        item.TargetUserId = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty;
        if (!Guid.TryParse(item.TargetUserId, out _))
        {
            rejectReason = "the 'sub' claim is missing or is not a user id (GUID)";
            return false;
        }

        item.TargetUserName = jwt.Claims.FirstOrDefault(c => c.Type == "userName")?.Value ?? item.TargetUserId;

        // 模拟 token 必带 act（RFC 8693，act.sub=操作者）。
        // 只有普通 token 没有 act，所以「必须有 act」能挡住「用链接把别人的浏览器切到攻击者自己的普通 token」。
        var act = jwt.Claims.FirstOrDefault(c => c.Type == "act")?.Value;
        if (!string.IsNullOrEmpty(act))
        {
            try
            {
                using var doc = JsonDocument.Parse(act);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("sub", out var sub)
                        && sub.ValueKind == JsonValueKind.String)
                    {
                        item.ActorUserId = sub.GetString() ?? string.Empty;
                    }

                    // RFC 8693 允许 act 携带附加属性。发送端带上 userName 时，
                    // 接收端横幅可以直接展示「发起人」姓名，而不必回退到本人身份比对或 Guid。
                    if (doc.RootElement.TryGetProperty("userName", out var actorUserName)
                        && actorUserName.ValueKind == JsonValueKind.String)
                    {
                        item.ActorUserName = actorUserName.GetString() ?? string.Empty;
                    }
                }
            }
            catch (JsonException)
            {
            }
        }

        if (string.IsNullOrEmpty(item.ActorUserId))
        {
            rejectReason = "the 'act' claim (impersonation actor) is missing or malformed";
            return false;
        }

        if (!Guid.TryParse(item.ActorUserId, out _))
        {
            rejectReason = "'act.sub' is not a user id (GUID)";
            return false;
        }

        // scope 可能是空格分隔的单个 claim，也可能被拆成多个 claim，两种都收集
        var scopes = new List<string>();
        foreach (var claim in jwt.Claims.Where(c => c.Type is "scope" or "scp"))
        {
            scopes.AddRange(claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        item.GrantedScopes = scopes.Distinct(StringComparer.Ordinal).ToList();

        // userinfo 端点要求 token 带 openid scope；缺了它 SSO 一定会拒绝，这里提前给出明确原因
        if (!item.GrantedScopes.Contains("openid", StringComparer.Ordinal))
        {
            rejectReason = "the token lacks the 'openid' scope, which is required to verify it against the identity provider " +
                           "(check the 'Scope' of the DCC 'Impersonation' config in the sender)";
            return false;
        }

        // 标准 OIDC access_token 必带 exp；没有 exp 的 token 不是后台签发的，而 fragment 里的 expires_in 来自 URL（可被任意构造），
        // 缺 exp 时若还信任它，会让一个坏 token 在 localStorage 里「长期有效」。直接拒绝。
        if (jwt.ValidTo == DateTime.MinValue)
        {
            rejectReason = "the 'exp' claim is missing";
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var expiresAt = new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero);
        if (expiresAt <= now)
        {
            rejectReason = "the token has already expired";
            return false;
        }

        // nbf 在未来（超出时钟偏差）：不是刚签发的 token
        if (jwt.ValidFrom != DateTime.MinValue && new DateTimeOffset(jwt.ValidFrom, TimeSpan.Zero) > now + ClockSkew)
        {
            rejectReason = "the token is not valid yet ('nbf' is in the future)";
            return false;
        }

        // JWT 自带 exp 时取它与 expires_in 的较早者，避免发送端 expires_in 与实际签发偏差
        if (expiresAt.ToUnixTimeSeconds() < item.ExpiresAtUnixSeconds)
        {
            item.ExpiresAtUnixSeconds = expiresAt.ToUnixTimeSeconds();
        }

        return true;
    }

    private static string Truncate(string value, int max = 128)
        => value.Length <= max ? value : value[..max] + "…";

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
            catch (JSException)
            {
            }
            _module = null;
        }

        _moduleLock.Dispose();
    }

    private sealed class CapturedFragment
    {
        public string AccessToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }

    /// <summary>JS <c>probeUserInfo</c> 的结果：HTTP 状态码（0=网络/CORS/超时）与 userinfo 的 sub。</summary>
    private sealed class UserInfoProbe
    {
        public int Status { get; set; }
        public string Sub { get; set; } = string.Empty;
    }

    /// <summary>localStorage 持久化模型（JSON）。</summary>
    public sealed class ImpersonationSessionItem
    {
        public string AccessToken { get; set; } = string.Empty;
        public long ExpiresAtUnixSeconds { get; set; }
        public string TargetUserId { get; set; } = string.Empty;
        public string TargetUserName { get; set; } = string.Empty;
        public string ActorUserId { get; set; } = string.Empty;
        public string ActorUserName { get; set; } = string.Empty;
        public List<string> GrantedScopes { get; set; } = new();
    }
}
