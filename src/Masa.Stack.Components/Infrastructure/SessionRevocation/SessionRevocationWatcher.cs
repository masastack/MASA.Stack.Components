// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

using System.Net.Http.Headers;
using Masa.Stack.Components.Extensions.OpenIdConnect;
using Masa.Stack.Components.Infrastructure.Impersonation;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Masa.Stack.Components.Infrastructure.SessionRevocation;

/// <summary>
/// 会话撤销探测：本人的登录会话在服务端被踢（管理员「踢出」在线用户）后，让浏览器端尽快下线。
/// <para>
/// 背景：踢人只能撤销 SSO 侧的会话（Cookie、refresh_token 等）。SPA 手里还握着一枚有效期最长一小时的 access_token，
/// 在它过期之前页面看上去一切正常、API 也能调通。本探测定期向 SSO 的 <c>GET /session/status</c> 出示本人的访问令牌，
/// SSO 会按踢人水位线判定它是否已被撤销；一旦判定为撤销，就清掉本地 OIDC 缓存并回到登录页。
/// </para>
/// <para>
/// 约束：
///  - 只用<b>本人</b>令牌（见 <see cref="ImpersonatedAuthenticationStateProvider.GetOwnAccessTokenAsync"/>），
///    模拟登录期间也不能用被模拟者的令牌，否则会漏掉操作者被踢、或误伤于被模拟者被踢；
///  - 这个标签页<b>没有本人的 OIDC 会话</b>时不探测：模拟登录是在新标签页里用被模拟者的短命令牌打开的，
///    而 OIDC 用户存在 sessionStorage（按标签页隔离），这里没有什么可问 SSO 的；
///    更不能为了「取令牌」去触发静默登录——那会在模拟登录的标签页里悄悄建出一个本人会话，失败时又会被误当成撤销；
///  - 绝不因瞬时故障登出：网络错误、超时、CORS、5xx、响应无法解析、令牌刚好过期，一律忽略，等下个周期；
///  - 「拿不到令牌」（续期失败）要<b>严格连续</b>出现 <see cref="TokenUnavailableThreshold"/> 次（中间夹着任何别的结果都清零），
///    并且 SSO 此刻确实可达（能取到 OIDC 发现文档），才视为被撤销——
///    它既可能是 refresh_token 已被删除（真被踢），也可能只是网络抖了一下、或 SSO 正好在重启；
///  - 只在判定为撤销时才动作；动作只执行一次。
/// </para>
/// 节奏：启动后稍等片刻做首次探测，之后每 <see cref="PollInterval"/> 一次；页面路由变化时按
/// <see cref="MinCheckInterval"/> 节流追加探测；业务代码（如管理员刚踢了可能是自己的会话）可调用 <see cref="CheckNowAsync"/> 立即探测。
/// 探测请求本身走 <see cref="HttpClient"/>，不依赖 JS；只有「有没有本人会话」的判断和下线时清缓存会用到 components.js 里的两个小函数。
/// </summary>
public sealed class SessionRevocationWatcher : IAsyncDisposable
{
    /// <summary>SSO 探测端点（相对 Authority）。与 SSO 的 SessionStatusController 路由是同一份契约，两边必须一起改。</summary>
    internal const string SessionStatusPath = "/session/status";

    /// <summary>
    /// 「拿不到令牌」<b>严格连续</b>出现多少次才<b>有资格</b>被视为撤销。取 2：一次可能是续期时的瞬时故障，
    /// 连续两个周期（约两分钟）都续不上，多半就是 refresh_token 已不在了。
    /// 达到次数后还要再确认 SSO 可达才会下线（见 <see cref="DiscoveryPath"/>）：SSO 都连不上时，续期失败说明不了任何问题。
    /// </summary>
    internal const int TokenUnavailableThreshold = 2;

    /// <summary>OIDC 发现文档（相对 Authority）。「拿不到令牌」够次数后，先用它确认 SSO 此刻可达，再决定是否下线。</summary>
    internal const string DiscoveryPath = "/.well-known/openid-configuration";

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private readonly Uri _endpoint;
    private readonly Uri _discoveryEndpoint;
    private readonly Func<CancellationToken, Task<OwnToken>> _accessTokenProvider;
    private readonly Func<Task> _onRevoked;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly Func<DateTimeOffset> _now;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _lifetime = new();

    private Timer? _timer;
    private DateTimeOffset _lastCheckAt = DateTimeOffset.MinValue;
    private int _checking;
    private int _tokenUnavailableCount;
    private bool _started;
    private bool _revoked;
    private bool _disposed;

    /// <summary>周期探测间隔。</summary>
    internal TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>启动后到首次探测的等待：让页面先稳定下来，同时让「回到一个已被踢的旧标签页」尽快发现。</summary>
    internal TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// 两次探测之间的最小间隔（路由触发与周期触发共用；<see cref="CheckNowAsync"/> 不受限）。
    /// 避免用户快速切换页面时把探测端点打成每次路由一次请求。
    /// </summary>
    internal TimeSpan MinCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>已判定为被撤销并执行过下线动作。之后不再探测。</summary>
    internal bool IsRevoked => _revoked;

    /// <summary>DI 构造：随 <c>AddMasaOpenIdConnectAsync</c> 注册，Authority 取自 <see cref="MasaOpenIdConnectOptions"/>。</summary>
    public SessionRevocationWatcher(
        MasaOpenIdConnectOptions options,
        IServiceProvider serviceProvider,
        IJSRuntime jsRuntime,
        ILogger<SessionRevocationWatcher> logger)
        : this(
            BuildEndpoint(options),
            BuildDiscoveryEndpoint(options),
            ct => AcquireOwnAccessTokenAsync(serviceProvider, jsRuntime),
            // 独立 HttpClient：不能走挂了授权 handler 的客户端——那条链路在令牌失效时本身就会去续期/重定向，探测会自己绊自己。
            new HttpClient(),
            ownsHttpClient: true,
            () => SignOutLocallyAsync(serviceProvider, jsRuntime, logger),
            () => DateTimeOffset.UtcNow,
            logger)
    {
    }

    /// <summary>可测试构造：令牌、请求通道、下线动作、时钟全部注入。</summary>
    internal SessionRevocationWatcher(
        Uri? endpoint,
        Uri? discoveryEndpoint,
        Func<CancellationToken, Task<OwnToken>> accessTokenProvider,
        HttpClient httpClient,
        bool ownsHttpClient,
        Func<Task> onRevoked,
        Func<DateTimeOffset> now,
        ILogger logger)
    {
        _endpoint = endpoint ?? throw new InvalidOperationException(
            $"{nameof(MasaOpenIdConnectOptions)}.{nameof(MasaOpenIdConnectOptions.Authority)} is not an absolute http(s) URL, " +
            "so the session revocation endpoint cannot be located.");
        _discoveryEndpoint = discoveryEndpoint ?? throw new InvalidOperationException(
            $"{nameof(MasaOpenIdConnectOptions)}.{nameof(MasaOpenIdConnectOptions.Authority)} is not an absolute http(s) URL, " +
            "so the OpenID Connect discovery document cannot be located.");
        _accessTokenProvider = accessTokenProvider;
        _httpClient = httpClient;
        _ownsHttpClient = ownsHttpClient;
        _onRevoked = onRevoked;
        _now = now;
        _logger = logger;
    }

    /// <summary>开始周期探测。重复调用无副作用；已判定撤销或已释放后调用无效。</summary>
    public void Start()
    {
        if (_started || _revoked || _disposed)
        {
            return;
        }

        _started = true;
        _timer = new Timer(_ => _ = CheckThrottledAsync(), null, InitialDelay, PollInterval);
    }

    /// <summary>停止周期探测（不影响 <see cref="CheckNowAsync"/>）。SLayout 释放时调用。</summary>
    public void Stop()
    {
        _started = false;
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>
    /// 路由变化时调用：距上次探测不足 <see cref="MinCheckInterval"/> 则忽略，否则后台追加一次探测。
    /// 未 <see cref="Start"/>（如还没通过登录校验）时不做任何事。
    /// </summary>
    public void NotifyLocationChanged()
    {
        if (!_started)
        {
            return;
        }

        _ = CheckThrottledAsync();
    }

    /// <summary>
    /// 立即探测一次（忽略节流）。适用于业务代码刚做了可能撤销本人会话的操作——
    /// 例如管理员在「在线用户」里踢了一个无法确认是否属于自己当前浏览器的会话——不必等下个周期。
    /// 不会抛异常。
    /// </summary>
    public Task CheckNowAsync() => CheckCoreAsync();

    /// <summary>
    /// 业务代码已<b>确知</b>本人会话被撤销（例如管理员刚在「在线用户」里把自己踢下线）时调用：
    /// 直接执行本地下线（结束模拟、清 OIDC 缓存、整页跳登录），不再向 SSO 探测。
    /// 与探测判定撤销走同一条下线路径，同样只执行一次；已释放则无效。不会抛异常。
    /// 只在<b>确知</b>时使用——拿不准是否影响本人（如踢的 sid 与当前浏览器的关系未知）请用 <see cref="CheckNowAsync"/> 让 SSO 裁决。
    /// </summary>
    public async Task SignOutNowAsync()
    {
        if (_disposed)
        {
            return;
        }

        await RevokeAsync("the application reported the session as revoked");
    }

    private Task CheckThrottledAsync()
    {
        if (_now() - _lastCheckAt < MinCheckInterval)
        {
            return Task.CompletedTask;
        }

        return CheckCoreAsync();
    }

    private async Task CheckCoreAsync()
    {
        if (_disposed || _revoked)
        {
            return;
        }

        // 上一次探测还没回来（慢网络）就不再叠加请求。
        if (Interlocked.Exchange(ref _checking, 1) == 1)
        {
            return;
        }

        try
        {
            _lastCheckAt = _now();

            var status = await ProbeAsync();

            // 「拿不到令牌」必须严格连续：任何别的结果（有效 / 无法判断 / 本标签页没有本人会话）都把计数清零。
            // 否则隔着几次「无法判断」的周期零星累加，就会把偶尔的续期失败攒成一次误下线。
            if (status != ProbeStatus.TokenUnavailable)
            {
                _tokenUnavailableCount = 0;
            }

            switch (status)
            {
                case ProbeStatus.TokenUnavailable:
                    _tokenUnavailableCount++;
                    _logger.LogDebug(
                        "Session revocation probe: no access token is available ({Count}/{Threshold})",
                        _tokenUnavailableCount, TokenUnavailableThreshold);
                    if (_tokenUnavailableCount >= TokenUnavailableThreshold)
                    {
                        // 续期失败也可能只是网络 / SSO 此刻不通（笔记本刚唤醒、切网络、SSO 重启）。
                        // 先确认 SSO 是通的：通着、却依然续不上，才说明本人的会话确实没了。
                        // 不通时不下线，也不清计数——SSO 一恢复，下个周期就能按真实情况判断。
                        if (await IsIdentityProviderReachableAsync())
                        {
                            await RevokeAsync("the access token could not be renewed while the identity provider is reachable");
                        }
                        else
                        {
                            _logger.LogDebug("Session revocation probe: the identity provider is unreachable; not signing out");
                        }
                    }

                    break;

                case ProbeStatus.Revoked:
                    await RevokeAsync("the identity provider reported the access token as revoked");
                    break;

                default:
                    // Active / Unknown：有效，或瞬时故障 / 无法判断，忽略，等下个周期（计数已在上面清零）。
                    break;
            }
        }
        catch (Exception ex)
        {
            // 探测本身出任何意外（含释放时的取消）都不能影响页面，更不能登出；
            // 也不能让异常逃出去：Timer/路由触发是 fire-and-forget，逃出去就成了未观察异常。
            _logger.LogDebug(ex, "Session revocation probe failed unexpectedly; ignored");
        }
        finally
        {
            Volatile.Write(ref _checking, 0);
        }
    }

    private async Task<ProbeStatus> ProbeAsync()
    {
        OwnToken own;
        try
        {
            own = await _accessTokenProvider(_lifetime.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // JS 互操作失败（含宿主缓存了旧版 components.js、没有 hasOidcUser）等：当作瞬时故障，不计入「拿不到令牌」。
            _logger.LogDebug(ex, "Session revocation probe: acquiring the access token failed; ignored");
            return ProbeStatus.Unknown;
        }

        if (own.NoOwnSession)
        {
            // 这个标签页没有本人会话（典型：模拟登录打开的新标签页）：没有什么可问 SSO 的，也绝不能当成撤销。
            _logger.LogDebug("Session revocation probe: this tab has no own OIDC session; skipped");
            return ProbeStatus.Unknown;
        }

        if (string.IsNullOrEmpty(own.Value))
        {
            return ProbeStatus.TokenUnavailable;
        }

        try
        {
            using var request = CreateRequest(_endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", own.Value);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            timeout.CancelAfter(RequestTimeout);

            using var response = await _httpClient.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                // 400（没带 Bearer）、网关 401/403/5xx 等：都不是「令牌被撤销」的权威答案。
                _logger.LogDebug("Session revocation probe: {Endpoint} answered HTTP {Status}; ignored", _endpoint, (int)response.StatusCode);
                return ProbeStatus.Unknown;
            }

            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            return ParseResponse(body);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or JSException)
        {
            // 网络错误 / CORS / 超时 / 响应不是 JSON：瞬时故障，忽略。
            _logger.LogDebug(ex, "Session revocation probe: request to {Endpoint} failed; ignored", _endpoint);
            return ProbeStatus.Unknown;
        }
    }

    /// <summary>
    /// SSO 此刻是否可达：能取到 OIDC 发现文档（2xx）。任何网络错误、超时、CORS 失败、非 2xx 都算不可达。
    /// 不带令牌——发现文档是公开的，也没有理由把本人令牌多发给一个端点。不会抛异常。
    /// </summary>
    private async Task<bool> IsIdentityProviderReachableAsync()
    {
        try
        {
            using var request = CreateRequest(_discoveryEndpoint);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            timeout.CancelAfter(RequestTimeout);

            // 只要响应头：是否可达不需要读文档体。
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JSException)
        {
            // 含释放时的取消：此时同样不应下线。
            _logger.LogDebug(ex, "Session revocation probe: request to {Endpoint} failed; the identity provider is treated as unreachable", _discoveryEndpoint);
            return false;
        }
    }

    /// <summary>
    /// 解析 <c>{"active":true}</c> / <c>{"active":false,"error":"invalid_token"|"expired_token"}</c>。
    /// 只有明确的 <c>invalid_token</c> 才算撤销；<c>expired_token</c> 说明令牌自然过期（时钟偏差或续期稍晚），
    /// 下个周期会拿到续期后的新令牌，不能登出。
    /// </summary>
    internal static ProbeStatus ParseResponse(string body)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return ProbeStatus.Unknown;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("active", out var active) ||
                active.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                return ProbeStatus.Unknown;
            }

            if (active.GetBoolean())
            {
                return ProbeStatus.Active;
            }

            var error = root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.String
                ? errorElement.GetString()
                : null;
            return string.Equals(error, "invalid_token", StringComparison.Ordinal)
                ? ProbeStatus.Revoked
                : ProbeStatus.Unknown;
        }
    }

    private async Task RevokeAsync(string reason)
    {
        if (_revoked)
        {
            return;
        }

        _revoked = true;
        Stop();
        _logger.LogInformation("The current session was revoked on the server ({Reason}); signing out locally", reason);

        try
        {
            await _onRevoked();
        }
        catch (Exception ex)
        {
            // 下线动作失败也只能记录：_revoked 已置位，不再重复探测，避免反复触发。
            _logger.LogError(ex, "Signing out locally after a revoked session failed");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        await _lifetime.CancelAsync();
        _lifetime.Dispose();
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    // ---------- DI 构造使用的默认实现 ----------

    /// <summary>探测端点：Authority 忽略首尾空白与尾部 '/'（与 <c>ImpersonationSession</c> 同款处理）；不是绝对 http(s) 地址返回 null。</summary>
    internal static Uri? BuildEndpoint(MasaOpenIdConnectOptions options) => BuildAuthorityUri(options, SessionStatusPath);

    /// <summary>OIDC 发现文档地址，规则同 <see cref="BuildEndpoint"/>。</summary>
    internal static Uri? BuildDiscoveryEndpoint(MasaOpenIdConnectOptions options) => BuildAuthorityUri(options, DiscoveryPath);

    private static Uri? BuildAuthorityUri(MasaOpenIdConnectOptions options, string path)
    {
        var authority = options.Authority?.Trim().TrimEnd('/');

        // 必须是 http(s)：在 Linux/macOS 上，"/relative/..." 会被 Uri 当成 file:// 绝对地址，
        // 光看 IsAbsoluteUri 会把一个根本不是 SSO 的地址放行。
        return !string.IsNullOrEmpty(authority)
               && Uri.TryCreate(authority + path, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri
            : null;
    }

    /// <summary>
    /// 探测与存活确认要的都是「此刻」的答案，所以显式声明 no-store，不让浏览器的 HTTP 缓存（或中间代理）拿旧响应顶替：
    /// 否则令牌被撤销之后，仍可能在一段时间内读到缓存里的 <c>{"active":true}</c>；SSO 恢复之后，也可能还读到缓存的失败。
    /// 仅对浏览器（Blazor WebAssembly 的 fetch）生效，其它宿主会忽略这个选项。
    /// </summary>
    private static HttpRequestMessage CreateRequest(Uri uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.SetBrowserRequestCache(BrowserRequestCache.NoStore);
        return request;
    }

    /// <summary>
    /// 取本人的访问令牌。有模拟装饰器就走它的本人通道；否则退回标准 <see cref="IAccessTokenProvider"/>。
    /// <para>
    /// 先<b>只读</b>地看这个标签页的 sessionStorage 里有没有本人的 OIDC 用户，没有就直接返回
    /// <see cref="OwnToken.NotApplicable"/>，一次令牌请求都不发。不能用 <c>GetOwnAuthenticationStateAsync()</c> 代替这一步：
    /// 框架的 JS 在没有用户时会趁机做一次静默登录（隐藏 iframe），RequestAccessToken 也一样——
    /// 在模拟登录的标签页里，它们要么悄悄建出一个本人会话（多出一份凭据、模拟结束后还会回落成操作者身份），
    /// 要么失败后只给出一个说不清原因的 RequiresRedirect。
    /// </para>
    /// <para>
    /// JS 调用失败（如宿主缓存了旧版 components.js，没有 <c>hasOidcUser</c>）时异常照常抛出，
    /// 由 <see cref="ProbeAsync"/> 当作瞬时故障忽略——这时宁可不探测，也不能退回「直接取令牌」。
    /// </para>
    /// </summary>
    internal static async Task<OwnToken> AcquireOwnAccessTokenAsync(IServiceProvider serviceProvider, IJSRuntime jsRuntime)
    {
        var options = serviceProvider.GetService<MasaOpenIdConnectOptions>();
        if (!await jsRuntime.InvokeAsync<bool>(OidcClientConsts.HAS_USER_FUNCTION, options?.Authority, options?.ClientId))
        {
            return OwnToken.NotApplicable;
        }

        AccessTokenResult result;
        if (serviceProvider.GetService<AuthenticationStateProvider>() is ImpersonatedAuthenticationStateProvider decorator)
        {
            result = await decorator.GetOwnAccessTokenAsync();
        }
        else if (serviceProvider.GetService<IAccessTokenProvider>() is { } provider)
        {
            result = await provider.RequestAccessToken();
        }
        else
        {
            // 宿主根本没有可问的令牌来源：无从探测，也无从判断是否被撤销。
            return OwnToken.NotApplicable;
        }

        return result.TryGetToken(out var accessToken) ? OwnToken.Of(accessToken.Value) : OwnToken.Unavailable;
    }

    /// <summary>
    /// 本地下线：先结束模拟（模拟 token 在 localStorage，不清会在重新登录后残留到过期），
    /// 再清 OIDC 令牌缓存，最后整页跳到登录。
    /// 用整页跳转（forceLoad）而不是仅清缓存：.NET 侧的 RemoteAuthenticationService 对用户有一段内存缓存，
    /// 只清 sessionStorage 会让页面在缓存过期前仍显示为已登录。
    /// 不走 /authentication/logout：本人会话已被撤销，不需要也不应再去 end-session 端点。
    /// 前两步任何一步失败都只记警告、继续往下走：不能因为清理没做成，就把用户留在一个已被撤销的会话里。
    /// </summary>
    internal static async Task SignOutLocallyAsync(IServiceProvider serviceProvider, IJSRuntime jsRuntime, ILogger logger)
    {
        try
        {
            if (serviceProvider.GetService<AuthenticationStateProvider>() is ImpersonatedAuthenticationStateProvider decorator)
            {
                await decorator.EndImpersonationAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ending the impersonation session after a revoked session failed; continuing");
        }

        try
        {
            var options = serviceProvider.GetService<MasaOpenIdConnectOptions>();
            await jsRuntime.InvokeVoidAsync(OidcClientConsts.CLEAR_SESSION_FUNCTION, options?.Authority, options?.ClientId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Clearing the cached OIDC tokens after a revoked session failed; continuing");
        }

        // 与 SLayout.CheckAuthenticatedAsync 的「未登录」处理一致（同一个 LOGIN_PATH）：MicroFrontendNavigationManager 会补上微前端路径前缀。
        var navigation = serviceProvider.GetService<MicroFrontendNavigationManager>()
            ?? (NavigationManager)serviceProvider.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(OidcClientConsts.LOGIN_PATH, forceLoad: true);
    }

    /// <summary>
    /// 取本人访问令牌的结果。用三态而不是 <c>string?</c>：「没有令牌」其实有两种完全不同的含义——
    /// 本人会话还在、只是续期失败（可能已被撤销），与这个标签页压根没有本人会话（如模拟登录打开的新标签页）。
    /// 后者没有什么可探测的，也绝不能算作撤销。
    /// </summary>
    internal readonly record struct OwnToken(string? Value, bool NoOwnSession)
    {
        /// <summary>本人会话在，但拿不到令牌（续期失败 / 需要重新登录）。</summary>
        public static OwnToken Unavailable => new(null, false);

        /// <summary>这个标签页没有本人会话（或没有令牌来源）：本轮跳过，既不请求也不计数。</summary>
        public static OwnToken NotApplicable => new(null, true);

        /// <summary>拿到了本人的访问令牌。</summary>
        public static OwnToken Of(string? value) => new(value, false);
    }

    internal enum ProbeStatus
    {
        /// <summary>无法判断（瞬时故障 / 非权威响应），忽略。</summary>
        Unknown,

        /// <summary>SSO 确认令牌有效。</summary>
        Active,

        /// <summary>SSO 明确判定令牌已被撤销。</summary>
        Revoked,

        /// <summary>本地拿不到访问令牌（续期失败）。</summary>
        TokenUnavailable,
    }
}
