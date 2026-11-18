// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace Masa.Stack.Components.Infrastructure.Impersonation;

/// <summary>
/// <see cref="AuthenticationStateProvider"/> 与 <see cref="IAccessTokenProvider"/> 的统一装饰器（同一实例兼任 UI 身份与 API 身份）。
///
/// 单实例多接口的原因：标准库对 IAccessTokenProvider 与 IRemoteAuthenticationService 的注册都是
/// 「sp.GetRequiredService&lt;AuthenticationStateProvider&gt;() 强转」的转发工厂——
/// 若把装饰拆成多个实例，这些转发工厂会拿 AuthenticationStateProvider 的装饰实例去强转而抛
/// InvalidCastException；本实例同时实现三个接口后，所有转发工厂取到的都是它，强转天然成立。
///
/// 模拟会话有效期间：
///  - GetAuthenticationStateAsync 返回"被模拟用户"的 ClaimsPrincipal（AuthorizeView/MasaUser/IUserContext 全部跟随）
///  - RequestAccessToken 返回被模拟用户的短命 access_token（Caller Bearer）
/// 两者从同一个 <see cref="ImpersonationSession"/>（同一个模拟 token）解出，杜绝"页面显示本人、操作是被模拟者"的半混合态。
/// 过期或结束 → 双双透传内层（本人 OIDC）。本人登出时联动清除模拟态。
/// </summary>
public class ImpersonatedAuthenticationStateProvider
    : AuthenticationStateProvider, IAccessTokenProvider, IRemoteAuthenticationService<RemoteAuthenticationState>, IDisposable
{
    /// <summary>附加到模拟身份上的标记 claim，UI 可据此显示"模拟中"。</summary>
    public const string ImpersonatedClaimType = "masa_impersonated";

    private const string RolesClaimType = "roles";
    private const string UserNameClaimType = "userName";

    private readonly AuthenticationStateProvider _inner;
    private readonly ImpersonationSession _session;
    private readonly ILogger<ImpersonatedAuthenticationStateProvider>? _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _warnedScopes = new(StringComparer.Ordinal);

    private IAccessTokenProvider InnerAccessTokenProvider =>
        _inner as IAccessTokenProvider
        ?? throw new InvalidOperationException(
            $"The inner {nameof(AuthenticationStateProvider)} must also implement {nameof(IAccessTokenProvider)}.");

    private IRemoteAuthenticationService<RemoteAuthenticationState> InnerRemoteAuthenticationService =>
        _inner as IRemoteAuthenticationService<RemoteAuthenticationState>
        ?? throw new InvalidOperationException(
            $"The inner {nameof(AuthenticationStateProvider)} must also implement {nameof(IRemoteAuthenticationService<RemoteAuthenticationState>)}.");

    // ---------- 登录/登出导航层（IRemoteAuthenticationService，全部透传内层） ----------

    public Task<RemoteAuthenticationResult<RemoteAuthenticationState>> SignInAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context)
        => InnerRemoteAuthenticationService.SignInAsync(context);

    public Task<RemoteAuthenticationResult<RemoteAuthenticationState>> CompleteSignInAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context)
        => InnerRemoteAuthenticationService.CompleteSignInAsync(context);

    public Task<RemoteAuthenticationResult<RemoteAuthenticationState>> SignOutAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context)
        => InnerRemoteAuthenticationService.SignOutAsync(context);

    public Task<RemoteAuthenticationResult<RemoteAuthenticationState>> CompleteSignOutAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context)
        => InnerRemoteAuthenticationService.CompleteSignOutAsync(context);

    public ImpersonatedAuthenticationStateProvider(
        AuthenticationStateProvider inner,
        ImpersonationSession session,
        ILogger<ImpersonatedAuthenticationStateProvider>? logger = null)
    {
        _inner = inner;
        _session = session;
        _logger = logger;
        _inner.AuthenticationStateChanged += OnInnerStateChanged;
        _session.Changed += OnSessionChanged;
    }

    /// <summary>
    /// 模拟会话建立/更换/结束（含其它 tab 经 storage 事件同步进来的变化）时，重新裁决并推送 UI 身份。
    /// 否则会话已被共享的 <see cref="ImpersonationSession"/> 更新、API 请求已换用新 token，
    /// 而 CascadingAuthenticationState 仍缓存着旧 principal，形成"页面是本人、操作是被模拟者"的半混合态。
    /// 回调里再次调用 GetAuthenticationStateAsync 不会递归：触发 Changed 之前会话的内存态已经更新完毕。
    /// </summary>
    private void OnSessionChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public void Dispose()
    {
        _inner.AuthenticationStateChanged -= OnInnerStateChanged;
        _session.Changed -= OnSessionChanged;
    }

    // ---------- API 身份层（IAccessTokenProvider） ----------

    public async ValueTask<AccessTokenResult> RequestAccessToken()
    {
        await EnsureSessionAsync();
        if (_session.IsActive)
        {
            return CreateSuccess();
        }
        return await InnerAccessTokenProvider.RequestAccessToken();
    }

    public async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        await EnsureSessionAsync();
        if (_session.IsActive)
        {
            // 模拟 token 是发送端按固定受众、固定 scope 签发的，无法按 options.Scopes 重新换取。
            // 请求了它并不具备的 scope 时，仍然返回它（并如实带上 GrantedScopes），由 API 以 403 给出权威答案。
            // 不能返回 RequiresRedirect：框架的 AccessTokenNotAvailableException.Redirect() 会据此导航到
            // InteractiveRequestUrl，而模拟态下没有任何合法的交互登录地址可给（空串会把用户带到站点根），
            // 且 AccessTokenResultStatus 只有 Success / RequiresRedirect 两种，没有「失败」可返回。
            WarnAboutMissingScopes(options.Scopes);
            return CreateSuccess();
        }

        return await InnerAccessTokenProvider.RequestAccessToken(options);
    }

    /// <summary>请求了模拟 token 未授予的 scope 时留一条告警（同一个 scope 只告警一次，避免每个请求都刷屏）。</summary>
    private void WarnAboutMissingScopes(IEnumerable<string>? requested)
    {
        if (_logger is null || requested is null)
        {
            return;
        }

        foreach (var scope in requested)
        {
            if (string.IsNullOrEmpty(scope) || _session.GrantedScopes.Contains(scope, StringComparer.Ordinal))
            {
                continue;
            }

            if (_warnedScopes.TryAdd(scope, 0))
            {
                _logger.LogWarning(
                    "A request asked for scope '{Scope}', which the impersonation token does not carry; " +
                    "the impersonation token is returned as-is and the API may answer 403. Granted scopes: {GrantedScopes}",
                    scope, string.Join(' ', _session.GrantedScopes));
            }
        }
    }

    private AccessTokenResult CreateSuccess() => new(
        AccessTokenResultStatus.Success,
        new AccessToken
        {
            Value = _session.AccessToken,
            Expires = _session.ExpiresAt,
            GrantedScopes = _session.GrantedScopes
        },
        string.Empty,
        null);

    private async Task EnsureSessionAsync()
    {
        await _session.EnsureLoadedAsync();
        if (!string.IsNullOrEmpty(_session.AccessToken) && _session.IsExpired)
        {
            await _session.EndAsync();
        }
    }

    // ---------- UI 身份层（AuthenticationStateProvider） ----------

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await _session.EnsureLoadedAsync();

        if (_session.IsActive)
        {
            try
            {
                return new AuthenticationState(BuildImpersonatedPrincipal(_session.AccessToken));
            }
            catch (Exception)
            {
                // token 无法解析（损坏/伪造）：清除会话并回落本人，绝不放行一个坏身份
                await _session.EndAsync();
            }
        }

        if (!string.IsNullOrEmpty(_session.AccessToken) && _session.IsExpired)
        {
            await _session.EndAsync();
        }

        return await _inner.GetAuthenticationStateAsync();
    }

    /// <summary>
    /// 立即结束模拟（横幅"结束模拟"按钮调用）。UI 刷新由 <see cref="OnSessionChanged"/> 统一推送：
    /// 会话确实被结束时 <see cref="ImpersonationSession.Changed"/> 会触发；本来就没有模拟则身份没变，无需通知。
    /// </summary>
    public Task EndImpersonationAsync() => _session.EndAsync();

    /// <summary>
    /// 操作者本人的（内层 OIDC）身份，不受模拟态影响。横幅展示「发起人」等场景使用：
    /// act.sub 与当前登录的本人一致时，可直接取本人的 userName 展示，避免横幅显示裸 Guid。
    /// </summary>
    public Task<AuthenticationState> GetOwnAuthenticationStateAsync() => _inner.GetAuthenticationStateAsync();

    private async void OnInnerStateChanged(Task<AuthenticationState> task)
    {
        try
        {
            var state = await task;
            if (state.User.Identity?.IsAuthenticated != true)
            {
                // 本人 OIDC 会话登出（SignOut）→ 模拟态必须一并清除，防止下次登录后残留
                await _session.EndAsync();
            }
        }
        catch (Exception)
        {
            // 状态透传不受清理失败影响
        }

        // 不能直接透传 inner 的 task：模拟进行中 inner 一旦 raise（OIDC 续期/其它 tab 变动），
        // 订阅者（CascadingAuthenticationState、MasaComponentsClaimsCache）会拿到「本人」principal，
        // 造成 UI 是本人、API 是被模拟者的半混合态。这里统一经由本装饰器重新裁决身份。
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// 从模拟 token 构造 ClaimsPrincipal。MASA 自定义 claims（sub/userName/staff_id/...）全量拷贝，
    /// roles JSON 数组拆分为多个 role claim，保证 AuthorizeView 角色判断、MasaUser 映射与正常登录一致。
    /// </summary>
    private ClaimsPrincipal BuildImpersonatedPrincipal(string accessToken)
    {
        var jwt = _tokenHandler.ReadJwtToken(accessToken);
        var claims = new List<Claim>();

        foreach (var claim in jwt.Claims)
        {
            if (claim.Type == RolesClaimType)
            {
                continue; // roles 单独拆分
            }

            // 全量原样拷贝（含 sub/userName/staff_id/current_team 等 MASA 自定义 claims，
            // MasaUser/IdentityClaimOptions 映射按原始 claim type 读取）
            claims.Add(new Claim(claim.Type, claim.Value, claim.ValueType, claim.Issuer, claim.OriginalIssuer));
        }

        foreach (var role in ParseRoles(jwt))
        {
            claims.Add(new Claim(RolesClaimType, role));
        }

        claims.Add(new Claim(ImpersonatedClaimType, "true"));
        claims.Add(new Claim("actor_user_id", _session.ActorUserId));

        var identity = new ClaimsIdentity(claims, "impersonation", UserNameClaimType, RolesClaimType);
        return new ClaimsPrincipal(identity);
    }

    private static IEnumerable<string> ParseRoles(JwtSecurityToken jwt)
    {
        foreach (var claim in jwt.Claims.Where(c => c.Type == RolesClaimType))
        {
            var value = claim.Value;
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (value.TrimStart().StartsWith("["))
            {
                string[]? parsed = null;
                try
                {
                    parsed = JsonSerializer.Deserialize<string[]>(value);
                }
                catch (JsonException)
                {
                    // 非法 JSON 当作单值处理
                }

                if (parsed is not null)
                {
                    foreach (var role in parsed)
                    {
                        if (!string.IsNullOrEmpty(role))
                        {
                            yield return role;
                        }
                    }

                    continue;
                }
            }

            yield return value;
        }
    }
}
