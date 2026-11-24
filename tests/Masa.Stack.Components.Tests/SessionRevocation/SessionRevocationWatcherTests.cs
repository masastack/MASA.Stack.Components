using System.Net;
using System.Text;
using Masa.Stack.Components.Extensions.OpenIdConnect;
using Masa.Stack.Components.Infrastructure;
using Masa.Stack.Components.Infrastructure.Identity;
using Masa.Stack.Components.Infrastructure.Impersonation;
using Masa.Stack.Components.Infrastructure.SessionRevocation;
using Masa.Stack.Components.Tests.Impersonation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Xunit;
using static Masa.Stack.Components.Infrastructure.SessionRevocation.SessionRevocationWatcher;

namespace Masa.Stack.Components.Tests.SessionRevocation;

public class SessionRevocationWatcherTests
{
    private const string Active = "{\"active\":true}";
    private const string Revoked = "{\"active\":false,\"error\":\"invalid_token\"}";

    /// <summary>完整的本地下线：结束模拟 → 清 OIDC 缓存 → 整页跳登录（顺序不能变）。</summary>
    private static readonly string[] FullSignOut =
    {
        "end-impersonation",
        "clear-oidc-cache",
        "navigate:/authentication/login:force",
    };

    // ------------------------------------------------------------ 响应解析

    [Theory]
    [InlineData("{\"active\":true}", "Active")]
    [InlineData("{\"active\":false,\"error\":\"invalid_token\"}", "Revoked")]
    // 令牌自然过期：下个周期会拿到续期后的令牌，不能登出
    [InlineData("{\"active\":false,\"error\":\"expired_token\"}", "Unknown")]
    // 没有明确的撤销结论 → 一律忽略
    [InlineData("{\"active\":false}", "Unknown")]
    [InlineData("{\"active\":false,\"error\":\"something_else\"}", "Unknown")]
    [InlineData("{\"active\":false,\"error\":null}", "Unknown")]
    [InlineData("{\"active\":\"true\"}", "Unknown")]
    [InlineData("{\"error\":\"invalid_token\"}", "Unknown")]
    [InlineData("[]", "Unknown")]
    [InlineData("\"invalid_token\"", "Unknown")]
    [InlineData("", "Unknown")]
    [InlineData("<html>502 Bad Gateway</html>", "Unknown")]
    public void ParseResponse_OnlyAnExplicitInvalidTokenCountsAsRevoked(string body, string expected)
        => Assert.Equal(Enum.Parse<ProbeStatus>(expected), ParseResponse(body));

    // ------------------------------------------------------------ 端点

    [Theory]
    [InlineData("https://sso.test", "https://sso.test/session/status")]
    [InlineData("https://sso.test/", "https://sso.test/session/status")]
    [InlineData("  https://sso.test//  ", "https://sso.test/session/status")]
    [InlineData("https://sso.test/base/", "https://sso.test/base/session/status")]
    public void Endpoint_IsBuiltFromTheAuthority(string authority, string expected)
        => Assert.Equal(expected, BuildEndpoint(new MasaOpenIdConnectOptions { Authority = authority })!.ToString());

    [Theory]
    [InlineData("https://sso.test", "https://sso.test/.well-known/openid-configuration")]
    [InlineData("https://sso.test/", "https://sso.test/.well-known/openid-configuration")]
    [InlineData("  https://sso.test//  ", "https://sso.test/.well-known/openid-configuration")]
    [InlineData("https://sso.test/base/", "https://sso.test/base/.well-known/openid-configuration")]
    public void DiscoveryEndpoint_IsBuiltFromTheAuthority(string authority, string expected)
        => Assert.Equal(expected, BuildDiscoveryEndpoint(new MasaOpenIdConnectOptions { Authority = authority })!.ToString());

    [Theory]
    [InlineData("http://localhost:5000", "http://localhost:5000/session/status")]
    [InlineData("HTTPS://SSO.TEST", "https://sso.test/session/status")]
    public void Endpoint_AcceptsHttpAndHttpsOnly_RegardlessOfCase(string authority, string expected)
        => Assert.Equal(expected, BuildEndpoint(new MasaOpenIdConnectOptions { Authority = authority })!.ToString());

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/relative")] // Linux/macOS 上 Uri 会把它当成 file:// 绝对地址，必须靠 scheme 校验挡掉
    [InlineData("sso.test")]
    [InlineData("file:///etc/sso")]
    [InlineData("ftp://sso.test")]
    [InlineData("javascript:alert(1)")]
    public void Endpoints_AreNullWhenTheAuthorityIsNotAnHttpUrl(string authority)
    {
        var options = new MasaOpenIdConnectOptions { Authority = authority };

        Assert.Null(BuildEndpoint(options));
        Assert.Null(BuildDiscoveryEndpoint(options));
    }

    [Fact]
    public void ConstructingWithAnInvalidAuthority_Throws()
        => Assert.Throws<InvalidOperationException>(() => new SessionRevocationWatcher(
            new MasaOpenIdConnectOptions { Authority = "" },
            new ServiceCollection().BuildServiceProvider(),
            new NoopJsRuntime(),
            NullLogger<SessionRevocationWatcher>.Instance));

    // ------------------------------------------------------------ 探测请求

    [Fact]
    public async Task Probe_SendsTheOwnTokenAsABearerToTheSessionStatusEndpoint()
    {
        var h = new Harness { Token = "own-token" };
        h.Respond(Active);

        await h.Watcher.CheckNowAsync();

        var request = Assert.Single(h.Handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://sso.test/session/status", request.Uri);
        Assert.Equal("Bearer own-token", request.Authorization);
        Assert.Equal(0, h.Revocations);
        Assert.False(h.Watcher.IsRevoked);
    }

    [Fact]
    public async Task Requests_AskTheBrowserNotToUseItsHttpCache()
    {
        // 探测要的是「此刻」的答案：撤销后不能被缓存的 {"active":true} 顶替；存活确认同理
        var probe = new Harness();
        probe.Respond(Active);
        await probe.Watcher.CheckNowAsync();
        Assert.Equal("no-store", Assert.Single(probe.Handler.ProbeRequests).BrowserCache);

        var liveness = new Harness { Token = null };
        await liveness.Watcher.CheckNowAsync();
        await liveness.Watcher.CheckNowAsync();
        Assert.Equal("no-store", Assert.Single(liveness.Handler.DiscoveryRequests).BrowserCache);
    }

    // ------------------------------------------------------------ 撤销判定

    [Fact]
    public async Task InvalidToken_SignsOutExactlyOnce_AndStopsProbing()
    {
        var h = new Harness();
        h.Respond(Revoked);

        await h.Watcher.CheckNowAsync();
        await h.Watcher.CheckNowAsync();
        await h.Watcher.CheckNowAsync();

        Assert.Equal(1, h.Revocations);
        Assert.True(h.Watcher.IsRevoked);
        Assert.Single(h.Handler.Requests); // 撤销后不再探测
    }

    [Fact]
    public async Task ActiveToken_NeverSignsOut()
    {
        var h = new Harness();
        h.Respond(Active);

        for (var i = 0; i < 5; i++)
        {
            await h.Watcher.CheckNowAsync();
        }

        Assert.Equal(0, h.Revocations);
        Assert.Equal(5, h.Handler.Requests.Count);
    }

    [Fact]
    public async Task ExpiredToken_IsIgnored()
    {
        var h = new Harness();
        h.Respond("{\"active\":false,\"error\":\"expired_token\"}");

        await h.Watcher.CheckNowAsync();

        Assert.Equal(0, h.Revocations);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task NonSuccessResponses_AreNeverTreatedAsRevoked(HttpStatusCode status)
    {
        // 即使响应体恰好写着 invalid_token：非 2xx 不是 SSO 的权威答案（网关/代理也会发 4xx/5xx）
        var h = new Harness();
        h.Respond(Revoked, status);

        await h.Watcher.CheckNowAsync();

        Assert.Equal(0, h.Revocations);
        Assert.False(h.Watcher.IsRevoked);
    }

    [Fact]
    public async Task TransientFailures_AreIgnoredAndTheNextCheckStillWorks()
    {
        var h = new Harness();

        h.Handler.Throw = new HttpRequestException("network down");
        await h.Watcher.CheckNowAsync();

        h.Handler.Throw = new TaskCanceledException("timeout");
        await h.Watcher.CheckNowAsync();

        h.Handler.Throw = null;
        h.Respond("not json at all");
        await h.Watcher.CheckNowAsync();

        Assert.Equal(0, h.Revocations);
        Assert.False(h.Watcher.IsRevoked);

        h.Respond(Revoked);
        await h.Watcher.CheckNowAsync();

        Assert.Equal(1, h.Revocations);
    }

    [Fact]
    public async Task JsInteropFailure_WhileSendingTheRequest_IsIgnored()
    {
        // 浏览器 fetch 失败（含 CORS）在 WASM 里表现为 HttpRequestException，这里再覆盖 JSException
        var h = new Harness();
        h.Handler.Throw = new JSException("fetch failed");

        await h.Watcher.CheckNowAsync();

        Assert.Equal(0, h.Revocations);
    }

    // ------------------------------------------------------------ 本标签页没有本人会话（模拟登录打开的新标签页）

    [Fact]
    public async Task NoOwnSession_IsSkipped_NoRequest_NoSignOut()
    {
        // 模拟登录是在新标签页里用被模拟者的短命令牌打开的，sessionStorage 里没有本人的 OIDC 用户：
        // 没有什么可问 SSO 的——既不发请求（没有可出示的令牌），也绝不能当成被撤销。
        var h = new Harness { NoOwnSession = true };
        h.Respond(Revoked);

        for (var i = 0; i < 5; i++)
        {
            await h.Watcher.CheckNowAsync();
        }

        Assert.Empty(h.Handler.Requests);
        Assert.Equal(0, h.Revocations);
        Assert.False(h.Watcher.IsRevoked);
    }

    [Fact]
    public async Task NoOwnSession_ThenAnOwnSessionAppears_ProbingResumes()
    {
        // 同一个标签页里后来真的登录了（有了本人会话）：探测要能恢复，且被撤销时照常下线
        var h = new Harness { NoOwnSession = true };
        h.Respond(Revoked);
        await h.Watcher.CheckNowAsync();
        Assert.Empty(h.Handler.Requests);

        h.NoOwnSession = false;
        await h.Watcher.CheckNowAsync();

        Assert.Single(h.Handler.Requests);
        Assert.Equal(1, h.Revocations);
    }

    // ------------------------------------------------------------ 令牌拿不到

    [Fact]
    public async Task TokenUnavailable_OnceIsIgnored_ButConsecutiveFailuresSignOut()
    {
        var h = new Harness { Token = null };

        await h.Watcher.CheckNowAsync();
        Assert.Equal(0, h.Revocations);
        Assert.Empty(h.Handler.Requests); // 第一次：没有令牌就没有请求，也还没到要确认 SSO 的时候

        await h.Watcher.CheckNowAsync();

        Assert.Equal(2, TokenUnavailableThreshold);
        Assert.Equal(1, h.Revocations);
        Assert.Empty(h.Handler.ProbeRequests); // 没有令牌，就不会向探测端点出示任何东西
    }

    [Fact]
    public async Task TokenUnavailable_ConfirmsTheIdentityProviderIsUpBeforeSigningOut()
    {
        var h = new Harness { Token = null };

        await h.Watcher.CheckNowAsync();
        await h.Watcher.CheckNowAsync();

        var request = Assert.Single(h.Handler.DiscoveryRequests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://sso.test/.well-known/openid-configuration", request.Uri);
        Assert.Null(request.Authorization); // 发现文档是公开的，不出示令牌
        Assert.Equal(1, h.Revocations);
    }

    [Fact]
    public async Task TokenUnavailable_WhileTheIdentityProviderIsUnreachable_DoesNotSignOut_UntilItIsBack()
    {
        // 笔记本刚唤醒 / 切网络 / SSO 重启：这时续不上令牌说明不了任何问题
        var h = new Harness { Token = null };
        h.Handler.DiscoveryThrow = new HttpRequestException("network down");

        for (var i = 0; i < 4; i++)
        {
            await h.Watcher.CheckNowAsync();
        }

        Assert.Equal(0, h.Revocations);
        Assert.False(h.Watcher.IsRevoked);
        Assert.Equal(3, h.Handler.DiscoveryRequests.Count); // 第 2、3、4 次各确认一次

        // SSO 回来了、令牌依然续不上：计数没有被清掉，下一个周期就按真实情况下线
        h.Handler.DiscoveryThrow = null;
        await h.Watcher.CheckNowAsync();

        Assert.Equal(1, h.Revocations);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task TokenUnavailable_WhenTheDiscoveryDocumentIsNot2xx_DoesNotSignOut(HttpStatusCode status)
    {
        var h = new Harness { Token = null };
        h.Handler.DiscoveryStatus = status;

        for (var i = 0; i < 3; i++)
        {
            await h.Watcher.CheckNowAsync();
        }

        Assert.Equal(0, h.Revocations);
        Assert.False(h.Watcher.IsRevoked);
    }

    [Fact]
    public async Task TokenUnavailable_WhenTheDiscoveryRequestTimesOutOrFails_DoesNotSignOut()
    {
        foreach (var failure in new Exception[] { new TaskCanceledException("timeout"), new JSException("fetch failed (CORS)") })
        {
            var h = new Harness { Token = null };
            h.Handler.DiscoveryThrow = failure;

            await h.Watcher.CheckNowAsync();
            await h.Watcher.CheckNowAsync();

            Assert.Equal(0, h.Revocations);
            Assert.False(h.Watcher.IsRevoked);
        }
    }

    [Theory]
    [InlineData("http-failure")]
    [InlineData("no-own-session")]
    [InlineData("token-provider-throws")]
    public async Task TokenUnavailable_MustBeStrictlyConsecutive_AnyOtherResultResetsTheCount(string interleaved)
    {
        var h = new Harness { Token = null };

        await h.Watcher.CheckNowAsync(); // 拿不到 1

        switch (interleaved)
        {
            case "http-failure": // 有令牌，但探测请求失败 → 无法判断
                h.Token = "own-token";
                h.Handler.Throw = new HttpRequestException("network down");
                break;
            case "no-own-session": // 这个标签页没有本人会话 → 不适用
                h.NoOwnSession = true;
                break;
            case "token-provider-throws": // 取令牌时 JS 互操作失败 → 无法判断
                h.TokenFailure = new InvalidOperationException("js not ready");
                break;
        }

        await h.Watcher.CheckNowAsync(); // 别的结果 → 清零

        h.Token = null;
        h.Handler.Throw = null;
        h.NoOwnSession = false;
        h.TokenFailure = null;
        await h.Watcher.CheckNowAsync(); // 拿不到 1（不是 2）

        Assert.Equal(0, h.Revocations);
        Assert.Empty(h.Handler.DiscoveryRequests); // 从没够过次数，自然不会去确认 SSO
    }

    [Fact]
    public async Task TokenUnavailable_CountIsResetByAnActiveProbe()
    {
        var h = new Harness { Token = null };
        h.Respond(Active);

        await h.Watcher.CheckNowAsync(); // 拿不到 1
        h.Token = "own-token";
        await h.Watcher.CheckNowAsync(); // 有效 → 清零
        h.Token = null;
        await h.Watcher.CheckNowAsync(); // 拿不到 1（不是 2）

        Assert.Equal(0, h.Revocations);
    }

    [Fact]
    public async Task TokenProviderThrowing_IsIgnoredAndNotCounted()
    {
        var h = new Harness { TokenFailure = new InvalidOperationException("js not ready") };

        for (var i = 0; i < 5; i++)
        {
            await h.Watcher.CheckNowAsync();
        }

        Assert.Equal(0, h.Revocations);
        Assert.False(h.Watcher.IsRevoked);
    }

    // ------------------------------------------------------------ 下线动作

    [Fact]
    public async Task SignOutAction_Failing_IsSwallowed_AndNotRepeated()
    {
        var h = new Harness { RevocationFailure = new InvalidOperationException("navigation failed") };
        h.Respond(Revoked);

        await h.Watcher.CheckNowAsync();
        await h.Watcher.CheckNowAsync();

        Assert.Equal(1, h.Revocations);
        Assert.True(h.Watcher.IsRevoked);
    }

    [Fact]
    public async Task SignOutNow_SignsOutWithoutProbing_AndOnlyOnce()
    {
        var h = new Harness();
        h.Respond(Revoked);

        await h.Watcher.SignOutNowAsync();
        await h.Watcher.SignOutNowAsync();
        await h.Watcher.CheckNowAsync(); // 已撤销：不再探测，也不会再下线一次

        Assert.Equal(1, h.Revocations);
        Assert.True(h.Watcher.IsRevoked);
        Assert.Empty(h.Handler.Requests);
    }

    [Fact]
    public async Task SignOutNow_AfterAProbeAlreadyRevoked_DoesNotSignOutTwice()
    {
        var h = new Harness();
        h.Respond(Revoked);

        await h.Watcher.CheckNowAsync();
        await h.Watcher.SignOutNowAsync();

        Assert.Equal(1, h.Revocations);
    }

    [Fact]
    public async Task SignOutNow_AfterDispose_IsANoOp()
    {
        var h = new Harness();

        await h.Watcher.DisposeAsync();
        await h.Watcher.SignOutNowAsync();

        Assert.Equal(0, h.Revocations);
    }

    // ------------------------------------------------------------ 并发 / 节流 / 生命周期

    [Fact]
    public async Task OverlappingChecks_DoNotStackRequests()
    {
        var h = new Harness();
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Handler.Gate = gate.Task;
        h.Respond(Active);

        var first = h.Watcher.CheckNowAsync();
        await Wait.UntilAsync(() => h.Handler.Requests.Count == 1);
        await h.Watcher.CheckNowAsync(); // 上一次还没回来：直接放弃
        gate.SetResult(true);
        await first;

        Assert.Single(h.Handler.Requests);
    }

    [Fact]
    public async Task LocationChange_BeforeStart_DoesNothing()
    {
        var h = new Harness();
        h.Respond(Active);

        h.Watcher.NotifyLocationChanged();
        await Task.Delay(50);

        Assert.Empty(h.Handler.Requests);
    }

    [Fact]
    public async Task LocationChange_IsThrottled_ButCheckNowIsNot()
    {
        var h = new Harness();
        h.Respond(Active);
        h.Watcher.Start();

        h.Watcher.NotifyLocationChanged();
        await Wait.UntilAsync(() => h.Handler.Requests.Count == 1);

        // 节流窗口内：路由再变也不探测
        h.Clock.Advance(TimeSpan.FromSeconds(10));
        h.Watcher.NotifyLocationChanged();
        await Task.Delay(50);
        Assert.Single(h.Handler.Requests);

        // CheckNow 不受节流
        await h.Watcher.CheckNowAsync();
        Assert.Equal(2, h.Handler.Requests.Count);

        // 过了窗口：路由触发恢复
        h.Clock.Advance(TimeSpan.FromSeconds(31));
        h.Watcher.NotifyLocationChanged();
        await Wait.UntilAsync(() => h.Handler.Requests.Count == 3);
    }

    [Fact]
    public async Task Start_IsIdempotent_AndChecksPeriodically()
    {
        var h = new Harness();
        h.Respond(Active);
        h.Watcher.InitialDelay = TimeSpan.FromMilliseconds(20);
        h.Watcher.PollInterval = TimeSpan.FromMilliseconds(20);
        h.Watcher.MinCheckInterval = TimeSpan.Zero;

        h.Watcher.Start();
        h.Watcher.Start(); // 第二次不应再建一个定时器

        await Wait.UntilAsync(() => h.Handler.Requests.Count >= 3);
        h.Watcher.Stop();
        var afterStop = h.Handler.Requests.Count;
        await Task.Delay(100);

        // Stop 之后不再有新的周期探测（允许停之前已在途的那一次）
        Assert.True(h.Handler.Requests.Count <= afterStop + 1);
    }

    [Fact]
    public async Task Revocation_StopsThePeriodicChecks()
    {
        var h = new Harness();
        h.Respond(Revoked);
        h.Watcher.InitialDelay = TimeSpan.FromMilliseconds(10);
        h.Watcher.PollInterval = TimeSpan.FromMilliseconds(10);
        h.Watcher.MinCheckInterval = TimeSpan.Zero;

        h.Watcher.Start();
        await Wait.UntilAsync(() => h.Revocations == 1);
        await Task.Delay(100);

        Assert.Equal(1, h.Revocations);
        Assert.Single(h.Handler.Requests);
    }

    [Fact]
    public async Task AfterDispose_NothingRuns_AndStartIsANoOp()
    {
        var h = new Harness();
        h.Respond(Revoked);

        await h.Watcher.DisposeAsync();
        h.Watcher.Start();
        await h.Watcher.CheckNowAsync();
        h.Watcher.NotifyLocationChanged();

        Assert.Empty(h.Handler.Requests);
        Assert.Equal(0, h.Revocations);
    }

    [Fact]
    public async Task Dispose_DoesNotDisposeAnHttpClientItDoesNotOwn()
    {
        var h = new Harness();
        h.Respond(Active);

        await h.Watcher.DisposeAsync();
        await h.Watcher.DisposeAsync(); // 幂等

        // 未拥有的 HttpClient 仍可用
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://sso.test/x");
        using var response = await h.Http.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ------------------------------------------------------------ 取本人令牌（DI 构造使用的默认实现）

    [Fact]
    public async Task AcquireOwnToken_WithoutAnOwnOidcUser_IsNotApplicable_AndNeverAsksForAToken()
    {
        // 模拟登录打开的新标签页：sessionStorage 里没有本人的 OIDC 用户。
        // 取令牌、读本人身份，在真实的 OIDC 服务里都会趁机做一次静默登录——一个都不能碰。
        var f = new DefaultsFixture();
        f.Js.HasOidcUser = false;
        await f.StartImpersonationAsync();

        var result = await AcquireOwnAccessTokenAsync(f.Services, f.Js);

        Assert.Equal(OwnToken.NotApplicable, result);
        Assert.Equal(0, f.Inner.AccessTokenRequests);
        Assert.Equal(0, f.Inner.AuthenticationStateRequests);
        Assert.Equal(new[] { OidcClientConsts.HAS_USER_FUNCTION }, f.Js.Calls); // 只读了一次存储，别的什么都没做
    }

    [Fact]
    public async Task AcquireOwnToken_WhileImpersonating_ReturnsTheOperatorsOwnToken()
    {
        var f = new DefaultsFixture();
        await f.StartImpersonationAsync();

        var result = await AcquireOwnAccessTokenAsync(f.Services, f.Js);

        Assert.Equal(OwnToken.Of("inner-token"), result); // 本人的令牌，不是被模拟者的短命令牌
        Assert.Equal(1, f.Inner.AccessTokenRequests);
        Assert.Equal(0, f.Inner.AuthenticationStateRequests);
    }

    [Fact]
    public async Task AcquireOwnToken_WithoutTheDecorator_FallsBackToTheStandardAccessTokenProvider()
    {
        var f = new DefaultsFixture(Wiring.PlainOidc);

        var result = await AcquireOwnAccessTokenAsync(f.Services, f.Js);

        Assert.Equal(OwnToken.Of("inner-token"), result);
        Assert.Equal(1, f.Inner.AccessTokenRequests);
    }

    [Fact]
    public async Task AcquireOwnToken_WithoutAnyTokenSource_IsNotApplicable()
    {
        // 宿主根本没有可问的令牌来源：无从探测，更不能当成「被撤销」
        var f = new DefaultsFixture(Wiring.Bare);

        var result = await AcquireOwnAccessTokenAsync(f.Services, f.Js);

        Assert.Equal(OwnToken.NotApplicable, result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AcquireOwnToken_WhenTheTokenCannotBeRenewed_IsUnavailable(bool decorated)
    {
        // 本人会话在、但续期失败（框架只会给出 RequiresRedirect）：这才是计入撤销判定的「拿不到令牌」
        var f = new DefaultsFixture(decorated ? Wiring.Decorated : Wiring.PlainOidc);
        f.Inner.RequireRedirect = true;

        var result = await AcquireOwnAccessTokenAsync(f.Services, f.Js);

        Assert.Equal(OwnToken.Unavailable, result);
        Assert.Equal(1, f.Inner.AccessTokenRequests);
    }

    [Fact]
    public async Task AcquireOwnToken_WhenTheJsCallFails_Throws_AndNeverFallsBackToAskingForAToken()
    {
        // 例如宿主缓存了旧版 components.js、没有 hasOidcUser：异常交给 ProbeAsync 当作瞬时故障忽略。
        // 这时宁可不探测，也不能退回「直接取令牌」去冒静默登录的险。
        var f = new DefaultsFixture();
        f.Js.HasOidcUserFailure = new JSException($"Could not find '{OidcClientConsts.HAS_USER_FUNCTION}'");

        await Assert.ThrowsAsync<JSException>(() => AcquireOwnAccessTokenAsync(f.Services, f.Js));

        Assert.Equal(0, f.Inner.AccessTokenRequests);
    }

    // ------------------------------------------------------------ 本地下线（DI 构造使用的默认实现）

    [Fact]
    public async Task SignOutLocally_EndsTheImpersonation_ThenClearsTheOidcCache_ThenNavigatesToLogin()
    {
        var f = new DefaultsFixture();
        await f.StartImpersonationAsync();

        await SignOutLocallyAsync(f.Services, f.Js, f.Log);

        Assert.Equal(FullSignOut, f.Events);
        Assert.False(f.Session.IsActive);
        Assert.False(f.Harness.Module.Storage.ContainsKey(ImpersonationSession.StorageKey)); // localStorage 里的模拟会话也清了
        Assert.Empty(f.Log.Warnings);
    }

    [Fact]
    public async Task SignOutLocally_StillClearsAndNavigates_WhenEndingTheImpersonationFails()
    {
        var f = new DefaultsFixture();
        await f.StartImpersonationAsync();
        f.Session.Changed += () => throw new InvalidOperationException("a subscriber failed"); // EndAsync 会把它抛出来

        await SignOutLocallyAsync(f.Services, f.Js, f.Log);

        Assert.Equal(FullSignOut, f.Events);
        Assert.Contains("Ending the impersonation session", Assert.Single(f.Log.Warnings));
    }

    [Fact]
    public async Task SignOutLocally_StillNavigates_WhenClearingTheOidcCacheFails()
    {
        var f = new DefaultsFixture();
        await f.StartImpersonationAsync();
        f.Js.ClearCacheFailure = new JSException("storage is disabled");

        await SignOutLocallyAsync(f.Services, f.Js, f.Log);

        Assert.Equal(FullSignOut, f.Events);
        Assert.Contains("Clearing the cached OIDC tokens", Assert.Single(f.Log.Warnings));
    }

    [Fact]
    public async Task SignOutLocally_StillNavigates_WhenBothPreliminaryStepsFail()
    {
        var f = new DefaultsFixture();
        await f.StartImpersonationAsync();
        f.Session.Changed += () => throw new InvalidOperationException("a subscriber failed");
        f.Js.ClearCacheFailure = new JSException("storage is disabled");

        await SignOutLocallyAsync(f.Services, f.Js, f.Log);

        Assert.Equal(FullSignOut, f.Events);
        Assert.Equal(2, f.Log.Warnings.Count());
    }

    [Fact]
    public async Task SignOutLocally_WithoutTheDecorator_SkipsEndingTheImpersonation()
    {
        // 自建认证装配（没有模拟装饰器）：无从结束模拟，但清缓存和跳登录照常
        var f = new DefaultsFixture(Wiring.PlainOidc);

        await SignOutLocallyAsync(f.Services, f.Js, f.Log);

        Assert.Equal(new[] { "clear-oidc-cache", "navigate:/authentication/login:force" }, f.Events);
        Assert.Empty(f.Log.Warnings);
    }

    [Fact]
    public async Task SignOutLocally_InAMicroFrontend_NavigatesThroughThePrefixedNavigationManager()
    {
        var f = new DefaultsFixture(microFrontendPrefix: "/pm/");

        await SignOutLocallyAsync(f.Services, f.Js, f.Log);

        Assert.Equal(new[] { "clear-oidc-cache", "navigate:/pm/authentication/login:force" }, f.Events);
    }

    // ------------------------------------------------------------ DI 构造的接线

    [Fact]
    public async Task DiConstructedWatcher_SignOutNow_RunsTheDefaultLocalSignOut()
    {
        var f = new DefaultsFixture();
        await f.StartImpersonationAsync();
        await using var watcher = new SessionRevocationWatcher(f.Options, f.Services, f.Js, f.Log);

        await watcher.SignOutNowAsync();

        Assert.True(watcher.IsRevoked);
        Assert.Equal(FullSignOut, f.Events);
    }

    [Fact]
    public async Task DiConstructedWatcher_UsesTheDocumentedCadence()
    {
        // 探测节奏是 MASA.Auth 设计文档（online-user-kick_plan.md §9.2）和容量估算采用的口径：
        // 启动 3 秒后首次探测、之后每 60 秒一次、路由触发的最短间隔 30 秒。
        // 改默认值必须连文档一起改；这个测试负责让两边不会悄悄分叉。
        var f = new DefaultsFixture();
        await using var watcher = new SessionRevocationWatcher(f.Options, f.Services, f.Js, f.Log);

        Assert.Equal(TimeSpan.FromSeconds(3), watcher.InitialDelay);
        Assert.Equal(TimeSpan.FromSeconds(60), watcher.PollInterval);
        Assert.Equal(TimeSpan.FromSeconds(30), watcher.MinCheckInterval);
    }

    [Fact]
    public async Task DiConstructedWatcher_WithoutAnOwnSession_DoesNothingAtAll()
    {
        // 模拟登录的新标签页：探测反复触发，既不取令牌、不发请求，也不会清缓存、结束模拟或跳转
        var f = new DefaultsFixture();
        f.Js.HasOidcUser = false;
        await f.StartImpersonationAsync();
        await using var watcher = new SessionRevocationWatcher(f.Options, f.Services, f.Js, f.Log);

        for (var i = 0; i < 4; i++)
        {
            await watcher.CheckNowAsync();
        }

        Assert.False(watcher.IsRevoked);
        Assert.Equal(4, f.Js.Calls.Count(c => c == OidcClientConsts.HAS_USER_FUNCTION));
        Assert.Equal(0, f.Inner.AccessTokenRequests);
        Assert.Empty(f.Events);
        Assert.True(f.Session.IsActive); // 模拟依然有效
    }

    // ------------------------------------------------------------ 工具

    /// <summary>认证服务的装配方式。</summary>
    private enum Wiring
    {
        /// <summary>与生产一致：模拟装饰器注册为 AuthenticationStateProvider。</summary>
        Decorated,

        /// <summary>自建认证装配：只有普通的 AuthenticationStateProvider + IAccessTokenProvider，没有装饰器。</summary>
        PlainOidc,

        /// <summary>宿主没有任何令牌来源。</summary>
        Bare,
    }

    /// <summary>
    /// 默认实现（取本人令牌 / 本地下线）的装配：假的本人 OIDC + 真的模拟会话与装饰器 + 可记录的 JS 与导航。
    /// <see cref="Events"/> 按发生顺序记录「结束模拟 / 清缓存 / 跳转」，用来断言下线的顺序。
    /// </summary>
    private sealed class DefaultsFixture
    {
        public List<string> Events { get; } = new();
        public FakeInnerProvider Inner { get; } = new();
        public SessionHarness Harness { get; } = new();
        public ListLogger<SessionRevocationWatcher> Log { get; } = new();
        public MasaOpenIdConnectOptions Options { get; } = new() { Authority = ImpersonationTestData.Authority };
        public RecordingJsRuntime Js { get; }
        public RecordingNavigationManager Navigation { get; }
        public IServiceProvider Services { get; }

        public ImpersonationSession Session => Harness.Session;

        public DefaultsFixture(Wiring wiring = Wiring.Decorated, string? microFrontendPrefix = null)
        {
            Js = new RecordingJsRuntime(Events);
            Navigation = new RecordingNavigationManager(Events);

            var services = new ServiceCollection();
            switch (wiring)
            {
                case Wiring.Decorated:
                    services.AddSingleton<AuthenticationStateProvider>(new ImpersonatedAuthenticationStateProvider(Inner, Session));
                    break;
                case Wiring.PlainOidc:
                    services.AddSingleton<AuthenticationStateProvider>(Inner);
                    services.AddSingleton<IAccessTokenProvider>(Inner);
                    break;
            }

            services.AddSingleton<NavigationManager>(Navigation);
            if (microFrontendPrefix is not null)
            {
                services.AddSingleton(new MicroFrontendNavigationManager(Navigation, microFrontendPrefix));
            }

            Services = services.BuildServiceProvider();
        }

        /// <summary>先完整捕获一个有效模拟会话；之后会话一旦结束就记一条事件。</summary>
        public async Task StartImpersonationAsync()
        {
            Assert.True(await Harness.CaptureAsync(ImpersonationTestData.CreateToken()));
            Session.Changed += () => Events.Add("end-impersonation");
        }
    }

    /// <summary>记录调用的 JS 运行时：只认识 hasOidcUser / clearOidcTokenCache 两个函数，其余一律报错。</summary>
    private sealed class RecordingJsRuntime : IJSRuntime
    {
        private readonly List<string> _events;

        public RecordingJsRuntime(List<string> events) => _events = events;

        /// <summary>本标签页的 sessionStorage 里有没有本人的 OIDC 用户。</summary>
        public bool HasOidcUser { get; set; } = true;

        public Exception? HasOidcUserFailure { get; set; }
        public Exception? ClearCacheFailure { get; set; }

        /// <summary>按顺序记录的 JS 函数名。</summary>
        public List<string> Calls { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Calls.Add(identifier);
            switch (identifier)
            {
                case OidcClientConsts.HAS_USER_FUNCTION:
                    return HasOidcUserFailure is not null
                        ? ValueTask.FromException<TValue>(HasOidcUserFailure)
                        : new ValueTask<TValue>((TValue)(object)HasOidcUser);

                case OidcClientConsts.CLEAR_SESSION_FUNCTION:
                    // 先记录再失败：事件序列要体现「这一步确实执行过」
                    _events.Add("clear-oidc-cache");
                    return ClearCacheFailure is not null
                        ? ValueTask.FromException<TValue>(ClearCacheFailure)
                        : default;

                default:
                    throw new NotSupportedException($"RecordingJsRuntime does not support '{identifier}'.");
            }
        }
    }

    /// <summary>记录导航的 NavigationManager；强制刷新记为 force。</summary>
    private sealed class RecordingNavigationManager : NavigationManager
    {
        private readonly List<string> _events;

        public RecordingNavigationManager(List<string> events)
        {
            _events = events;
            Initialize("https://app.test/", "https://app.test/");
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
            => _events.Add($"navigate:{uri}:{(options.ForceLoad ? "force" : "soft")}");
    }

    private sealed class Harness
    {
        private int _revocations;

        public StubHandler Handler { get; } = new();
        public FakeClock Clock { get; } = new();
        public HttpClient Http { get; }
        public SessionRevocationWatcher Watcher { get; }

        /// <summary>null = 本人会话在、但拿不到令牌（续期失败）。</summary>
        public string? Token { get; set; } = "token";

        /// <summary>true = 这个标签页没有本人的 OIDC 会话（如模拟登录打开的新标签页），令牌提供者返回「不适用」。</summary>
        public bool NoOwnSession { get; set; }

        public Exception? TokenFailure { get; set; }
        public Exception? RevocationFailure { get; set; }

        public int Revocations => Volatile.Read(ref _revocations);

        public Harness()
        {
            Http = new HttpClient(Handler);
            Watcher = new SessionRevocationWatcher(
                new Uri("https://sso.test/session/status"),
                new Uri("https://sso.test/.well-known/openid-configuration"),
                _ => TokenFailure is not null
                    ? Task.FromException<OwnToken>(TokenFailure)
                    : Task.FromResult(NoOwnSession ? OwnToken.NotApplicable : OwnToken.Of(Token)),
                Http,
                ownsHttpClient: false,
                () =>
                {
                    Interlocked.Increment(ref _revocations);
                    return RevocationFailure is not null ? Task.FromException(RevocationFailure) : Task.CompletedTask;
                },
                () => Clock.Now,
                NullLogger.Instance)
            {
                // 默认不让定时器干扰：需要定时行为的测试自己调短
                InitialDelay = TimeSpan.FromHours(1),
                PollInterval = TimeSpan.FromHours(1),
            };
        }

        public void Respond(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            Handler.Status = status;
            Handler.Body = body;
        }
    }

    /// <param name="BrowserCache">请求上声明的浏览器 fetch 缓存策略（<c>SetBrowserRequestCache</c> 写入的 cache 选项）。</param>
    private sealed record RecordedRequest(HttpMethod Method, string Uri, string? Authorization, string? BrowserCache);

    private sealed class StubHandler : HttpMessageHandler
    {
        private const string DiscoverySuffix = "/.well-known/openid-configuration";

        private readonly object _gate = new();
        private readonly List<RecordedRequest> _requests = new();

        /// <summary>探测端点（session/status）的响应。</summary>
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public string Body { get; set; } = Active;
        public Exception? Throw { get; set; }

        /// <summary>非 null 时探测请求会一直等到它完成（模拟慢网络）。</summary>
        public Task? Gate { get; set; }

        /// <summary>发现文档（SSO 存活确认）的响应；与探测端点分开配置，默认可达。</summary>
        public HttpStatusCode DiscoveryStatus { get; set; } = HttpStatusCode.OK;
        public Exception? DiscoveryThrow { get; set; }

        /// <summary>收到的全部请求（探测 + 存活确认）。</summary>
        public IReadOnlyList<RecordedRequest> Requests
        {
            get
            {
                lock (_gate)
                {
                    return _requests.ToList();
                }
            }
        }

        /// <summary>只含探测端点（session/status）的请求。</summary>
        public IReadOnlyList<RecordedRequest> ProbeRequests => Requests.Where(r => !IsDiscovery(r.Uri)).ToList();

        /// <summary>只含发现文档（SSO 存活确认）的请求。</summary>
        public IReadOnlyList<RecordedRequest> DiscoveryRequests => Requests.Where(r => IsDiscovery(r.Uri)).ToList();

        private static bool IsDiscovery(string uri) => uri.EndsWith(DiscoverySuffix, StringComparison.Ordinal);

        // SetBrowserRequestCache 把选项写进 request.Options 里名为 WebAssemblyFetchOptions 的字典（键 "cache"）；
        // 非浏览器宿主不会读它，但选项本身照常记录，正好用来断言。
        private static string? BrowserCache(HttpRequestMessage request)
            => ((IDictionary<string, object?>)request.Options).TryGetValue("WebAssemblyFetchOptions", out var options)
               && options is System.Collections.IDictionary fetchOptions
                ? fetchOptions["cache"]?.ToString()
                : null;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!.ToString();
            lock (_gate)
            {
                _requests.Add(new RecordedRequest(request.Method, uri, request.Headers.Authorization?.ToString(), BrowserCache(request)));
            }

            if (IsDiscovery(uri))
            {
                if (DiscoveryThrow is not null)
                {
                    throw DiscoveryThrow;
                }

                return new HttpResponseMessage(DiscoveryStatus) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
            }

            if (Gate is not null)
            {
                await Gate;
            }

            if (Throw is not null)
            {
                throw Throw;
            }

            return new HttpResponseMessage(Status) { Content = new StringContent(Body, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class FakeClock
    {
        private long _ticks = new DateTimeOffset(2026, 11, 19, 8, 0, 0, TimeSpan.Zero).Ticks;

        public DateTimeOffset Now => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);

        public void Advance(TimeSpan by) => Interlocked.Add(ref _ticks, by.Ticks);
    }

    private sealed class NoopJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => default;
    }

    private static class Wait
    {
        public static async Task UntilAsync(Func<bool> condition, int timeoutMilliseconds = 3000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException("The expected condition was not reached in time.");
                }

                await Task.Delay(10);
            }
        }
    }
}
