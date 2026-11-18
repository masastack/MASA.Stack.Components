using Masa.Stack.Components.Extensions.OpenIdConnect;
using Masa.Stack.Components.Infrastructure.Impersonation;
using Microsoft.JSInterop;
using System.Text.Json;
using Xunit;
using static Masa.Stack.Components.Tests.Impersonation.ImpersonationTestData;

namespace Masa.Stack.Components.Tests.Impersonation;

public class ImpersonationSessionTests
{
    // ------------------------------------------------------------ 捕获：通过路径

    [Fact]
    public async Task Capture_AcceptsAWellFormedTokenThatTheIdentityProviderVerifies()
    {
        var h = new SessionHarness();
        var token = CreateToken();

        var captured = await h.CaptureAsync(token);

        Assert.True(captured);
        Assert.True(h.Session.IsActive);
        Assert.Equal(TargetId, h.Session.TargetUserId);
        Assert.Equal(ActorId, h.Session.ActorUserId);
        Assert.Equal("Target User", h.Session.TargetUserName);
        Assert.Equal(new[] { "openid", "profile" }, h.Session.GrantedScopes);
        Assert.Equal(token, h.Session.AccessToken);
        Assert.True(h.Module.Storage.ContainsKey(ImpersonationSession.StorageKey));

        // 向 SSO 出示的就是这个 token，打到 {authority}/connect/userinfo
        var probe = Assert.Single(h.Module.Probes);
        Assert.Equal("https://sso.test/connect/userinfo", probe.Endpoint);
        Assert.Equal(token, probe.Token);
        Assert.True(probe.TimeoutMs > 0);
    }

    [Fact]
    public async Task Capture_RaisesChangedExactlyOnce()
    {
        var h = new SessionHarness();
        var changed = 0;
        h.Session.Changed += () => changed++;

        await h.CaptureAsync(CreateToken());

        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task Capture_ReturnsFalseWithoutAnyNetworkCallWhenThereIsNoFragment()
    {
        var h = new SessionHarness();
        h.Module.Fragment = null;

        Assert.False(await h.Session.CaptureFromUrlAsync());

        Assert.Empty(h.Module.Probes);
        Assert.Empty(h.Module.Storage);
    }

    [Fact]
    public async Task Capture_IssuerComparisonIgnoresTrailingSlashAndCase()
    {
        var h = new SessionHarness(new MasaOpenIdConnectOptions { Authority = "https://SSO.test/" });

        Assert.True(await h.CaptureAsync(CreateToken(new TokenSpec { Issuer = "https://sso.test" })));

        Assert.Equal("https://SSO.test/connect/userinfo", Assert.Single(h.Module.Probes).Endpoint);
    }

    [Fact]
    public async Task Capture_UsesTheConfiguredIssuerWhenItDiffersFromTheAuthority()
    {
        // 应用经内网地址访问 SSO，但 SSO 以对外域名作为 iss
        var options = new MasaOpenIdConnectOptions
        {
            Authority = "https://sso.internal",
            Issuer = "https://sso.test/",
        };

        var accepted = new SessionHarness(options);
        Assert.True(await accepted.CaptureAsync(CreateToken(new TokenSpec { Issuer = "https://sso.test" })));
        Assert.Equal("https://sso.internal/connect/userinfo", Assert.Single(accepted.Module.Probes).Endpoint);

        // 配置了 Issuer 之后，iss 等于 Authority 反而不再被信任
        var rejected = new SessionHarness(options);
        Assert.False(await rejected.CaptureAsync(CreateToken(new TokenSpec { Issuer = "https://sso.internal" })));
        Assert.Empty(rejected.Module.Probes);
    }

    [Fact]
    public async Task Capture_ExpiryIsTheEarlierOfExpiresInAndTheJwtExp()
    {
        var shortExpiresIn = new SessionHarness();
        await shortExpiresIn.CaptureAsync(CreateToken(), expiresIn: 60);
        AssertExpiresIn(shortExpiresIn.Session, TimeSpan.FromSeconds(60));

        var shortExp = new SessionHarness();
        await shortExp.CaptureAsync(CreateToken(new TokenSpec { Expires = DateTime.UtcNow.AddMinutes(2) }), expiresIn: 3600);
        AssertExpiresIn(shortExp.Session, TimeSpan.FromMinutes(2));

        static void AssertExpiresIn(ImpersonationSession session, TimeSpan expected)
        {
            var remaining = session.ExpiresAt - DateTimeOffset.Now;
            Assert.InRange(remaining.TotalSeconds, expected.TotalSeconds - 5, expected.TotalSeconds + 1);
        }
    }

    // ------------------------------------------------------------ 捕获：本地预检拒绝（不发任何网络请求）

    [Theory]
    [InlineData("no-act", "act")]
    [InlineData("act-is-not-an-object", "act")]
    [InlineData("act-without-sub", "act")]
    [InlineData("act-sub-not-guid", "act.sub")]
    [InlineData("sub-missing", "sub")]
    [InlineData("sub-not-guid", "sub")]
    [InlineData("foreign-issuer", "issuer")]
    [InlineData("no-exp", "exp")]
    [InlineData("already-expired", "expired")]
    [InlineData("nbf-in-the-future", "nbf")]
    [InlineData("no-openid-scope", "openid")]
    public async Task Capture_RejectsTokensThatFailTheLocalPrecheck(string caseName, string reasonFragment)
    {
        var h = new SessionHarness();
        var token = CreateToken(Spec(caseName));

        var captured = await h.CaptureAsync(token);

        Assert.False(captured);
        Assert.False(h.Session.IsActive);
        Assert.Empty(h.Session.AccessToken);
        Assert.Empty(h.Module.Storage);
        Assert.Empty(h.Module.Probes); // 预检挡住的 token 根本不会发给 SSO

        var warning = Assert.Single(h.Log.Warnings);
        Assert.Contains(reasonFragment, warning, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(token, warning, StringComparison.Ordinal); // 日志绝不能带 token
    }

    private static TokenSpec Spec(string caseName) => caseName switch
    {
        "no-act" => new TokenSpec { IncludeAct = false },
        "act-is-not-an-object" => new TokenSpec { ActRaw = "[\"x\"]" },
        "act-without-sub" => new TokenSpec { ActRaw = "{\"name\":\"x\"}" },
        "act-sub-not-guid" => new TokenSpec { ActSub = "attacker" },
        "sub-missing" => new TokenSpec { Sub = null },
        "sub-not-guid" => new TokenSpec { Sub = "SuperAdmin" },
        "foreign-issuer" => new TokenSpec { Issuer = "https://evil.example" },
        "no-exp" => new TokenSpec { Expires = null },
        "already-expired" => new TokenSpec { NotBefore = DateTime.UtcNow.AddMinutes(-10), Expires = DateTime.UtcNow.AddMinutes(-1) },
        "nbf-in-the-future" => new TokenSpec { NotBefore = DateTime.UtcNow.AddHours(1), Expires = DateTime.UtcNow.AddHours(2) },
        "no-openid-scope" => new TokenSpec { Scope = "profile" },
        _ => throw new ArgumentOutOfRangeException(nameof(caseName)),
    };

    [Fact]
    public async Task Capture_ToleratesSmallClockSkewOnNotBefore()
    {
        var h = new SessionHarness();

        // 本地时钟比 SSO 慢 2 分钟：nbf 看起来在未来 2 分钟，仍应接受
        var token = CreateToken(new TokenSpec { NotBefore = DateTime.UtcNow.AddMinutes(2) });

        Assert.True(await h.CaptureAsync(token));
    }

    [Fact]
    public async Task Capture_ReadsTheActorNameFromTheActClaim_AndPersistsIt()
    {
        var h = new SessionHarness();
        var token = CreateToken(new TokenSpec { ActUserName = "Operator One" });

        Assert.True(await h.CaptureAsync(token));

        Assert.Equal("Operator One", h.Session.ActorUserName);

        // 发起人姓名随会话一起持久化，整页刷新后无需重新解析
        var stored = JsonSerializer.Deserialize<ImpersonationSession.ImpersonationSessionItem>(
            h.Module.Storage[ImpersonationSession.StorageKey],
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("Operator One", stored!.ActorUserName);
    }

    [Fact]
    public async Task Capture_LeavesTheActorNameEmptyWhenTheActClaimOmitsIt()
    {
        var h = new SessionHarness();

        Assert.True(await h.CaptureAsync(CreateToken()));

        Assert.Equal(string.Empty, h.Session.ActorUserName);
    }

    [Fact]
    public async Task EnsureLoaded_RestoresTheActorName()
    {
        var h = new SessionHarness();
        StoreSession(h.Module, CreateToken(), DateTimeOffset.UtcNow.AddMinutes(5), actorUserName: "Operator One");

        await h.Session.EnsureLoadedAsync();

        Assert.True(h.Session.IsActive);
        Assert.Equal("Operator One", h.Session.ActorUserName);
    }

    [Theory]
    [InlineData("not-a-jwt")]
    [InlineData("a.b.c")]
    public async Task Capture_RejectsStringsThatAreNotJwts(string garbage)
    {
        var h = new SessionHarness();

        Assert.False(await h.CaptureAsync(garbage));

        Assert.Empty(h.Module.Probes);
        Assert.Empty(h.Module.Storage);
    }

    [Fact]
    public async Task Capture_RejectsEverythingWhenNoOidcConfigurationIsAvailable()
    {
        // 没有可信的 SSO 地址就无法验证：宁可不启用，也不放行未验证的 token
        var h = new SessionHarness(withoutOptions: true);

        Assert.False(await h.CaptureAsync(CreateToken()));

        Assert.Empty(h.Module.Probes);
        Assert.Empty(h.Module.Storage);
        Assert.Contains("authority", Assert.Single(h.Log.Warnings), StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------ 捕获：SSO 验证（userinfo）

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(500)]
    public async Task Capture_RejectsWhenTheIdentityProviderDoesNotAnswer200(int status)
    {
        var h = new SessionHarness();
        h.Module.ProbeStatus = status;

        Assert.False(await h.CaptureAsync(CreateToken()));

        Assert.False(h.Session.IsActive);
        Assert.Empty(h.Module.Storage);
        Assert.Contains($"HTTP {status}", Assert.Single(h.Log.Warnings));
    }

    [Fact]
    public async Task Capture_RejectsWhenTheIdentityProviderIsUnreachable()
    {
        // 网络错误 / CORS 失败 / 超时：fail closed
        var h = new SessionHarness();
        h.Module.ProbeStatus = 0;

        Assert.False(await h.CaptureAsync(CreateToken()));

        Assert.Empty(h.Module.Storage);
        Assert.Contains("unreachable", Assert.Single(h.Log.Warnings));
    }

    [Fact]
    public async Task Capture_RejectsWhenTheProbeItselfFails()
    {
        var h = new SessionHarness();
        h.Module.ProbeException = new JSException("boom");

        Assert.False(await h.CaptureAsync(CreateToken()));

        Assert.Empty(h.Module.Storage);
        Assert.False(h.Session.IsActive);
    }

    [Fact]
    public async Task Capture_RejectsWhenTheIdentityProviderReportsADifferentSubject()
    {
        var h = new SessionHarness();
        h.Module.ProbeSub = "99999999-9999-9999-9999-999999999999";

        Assert.False(await h.CaptureAsync(CreateToken()));

        Assert.Empty(h.Module.Storage);
        Assert.Contains("subject", Assert.Single(h.Log.Warnings));
    }

    [Fact]
    public async Task Capture_RejectsWhenTheIdentityProviderReturnsNoSubject()
    {
        var h = new SessionHarness();
        h.Module.ProbeSub = string.Empty;

        Assert.False(await h.CaptureAsync(CreateToken()));
    }

    [Fact]
    public async Task Capture_ComparesTheSubjectCaseInsensitively()
    {
        var h = new SessionHarness();
        h.Module.ProbeSub = TargetId.ToUpperInvariant();

        Assert.True(await h.CaptureAsync(CreateToken(new TokenSpec { Sub = TargetId })));
    }

    // ------------------------------------------------------------ JS 模块不可用

    [Fact]
    public async Task Capture_ReturnsFalseAndStopsRetryingWhenTheScriptCannotBeLoaded()
    {
        var h = new SessionHarness();
        h.Js.ImportFailure = new JSException("404");

        Assert.False(await h.CaptureAsync(CreateToken()));
        Assert.False(await h.CaptureAsync(CreateToken()));

        Assert.Equal(1, h.Js.ImportCount); // 只告警一次，之后不再反复尝试
        Assert.Single(h.Log.Entries, e => e.Level == Microsoft.Extensions.Logging.LogLevel.Error);
    }

    // ------------------------------------------------------------ 恢复 / 结束 / 跨 tab 同步

    [Fact]
    public async Task EnsureLoaded_RestoresAStoredSessionThatIsStillValid()
    {
        var h = new SessionHarness();
        StoreSession(h.Module, CreateToken(), DateTimeOffset.UtcNow.AddMinutes(5));

        await h.Session.EnsureLoadedAsync();

        Assert.True(h.Session.IsLoaded);
        Assert.True(h.Session.IsActive);
        Assert.Equal(TargetId, h.Session.TargetUserId);
    }

    [Fact]
    public async Task EnsureLoaded_ClearsAStoredSessionThatHasExpired()
    {
        var h = new SessionHarness();
        StoreSession(h.Module, CreateToken(), DateTimeOffset.UtcNow.AddMinutes(-1));

        await h.Session.EnsureLoadedAsync();

        Assert.False(h.Session.IsActive);
        Assert.Empty(h.Session.AccessToken);
        Assert.Empty(h.Module.Storage);
    }

    [Fact]
    public async Task EnsureLoaded_WithNothingStoredIsInactive()
    {
        var h = new SessionHarness();

        await h.Session.EnsureLoadedAsync();

        Assert.True(h.Session.IsLoaded);
        Assert.False(h.Session.IsActive);
    }

    [Fact]
    public async Task End_ClearsMemoryAndStorage_AndRaisesChangedOnlyWhenThereWasASession()
    {
        var h = new SessionHarness();
        await h.CaptureAsync(CreateToken());
        var changed = 0;
        h.Session.Changed += () => changed++;

        await h.Session.EndAsync();

        Assert.False(h.Session.IsActive);
        Assert.Empty(h.Session.AccessToken);
        Assert.Empty(h.Session.TargetUserId);
        Assert.Empty(h.Module.Storage);
        Assert.Equal(1, changed);

        await h.Session.EndAsync(); // 已经没有会话：不再通知
        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task Refresh_NotifiesWhenAnotherTabEndsTheSession()
    {
        var h = new SessionHarness();
        await h.CaptureAsync(CreateToken());
        var changed = 0;
        h.Session.Changed += () => changed++;

        h.Module.Storage.Clear(); // 另一个 tab 结束了模拟
        await h.Session.RefreshFromStorageAsync();

        Assert.False(h.Session.IsActive);
        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task Refresh_NotifiesWhenAnotherTabSwitchesToADifferentUser()
    {
        var h = new SessionHarness();
        await h.CaptureAsync(CreateToken());
        var changed = 0;
        h.Session.Changed += () => changed++;

        // 前后都是「有会话」，但换了人：订阅者仍必须收到通知
        var other = "33333333-3333-3333-3333-333333333333";
        var otherToken = CreateToken(new TokenSpec { Sub = other });
        StoreSession(h.Module, otherToken, DateTimeOffset.UtcNow.AddMinutes(5), targetUserId: other);
        await h.Session.RefreshFromStorageAsync();

        Assert.True(h.Session.IsActive);
        Assert.Equal(other, h.Session.TargetUserId);
        Assert.Equal(otherToken, h.Session.AccessToken);
        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task Refresh_DoesNotNotifyWhenNothingChanged()
    {
        var h = new SessionHarness();
        await h.CaptureAsync(CreateToken());
        var changed = 0;
        h.Session.Changed += () => changed++;

        await h.Session.RefreshFromStorageAsync();

        Assert.Equal(0, changed);
    }

    [Fact]
    public async Task Refresh_RecordsWhetherTheStoredSessionWasExpired()
    {
        // 横幅判别「自然过期 / 被其它 tab 结束」依赖这个事实：必须在 Changed 链清除 token 前记录
        var h = new SessionHarness();
        await h.CaptureAsync(CreateToken());
        Assert.False(h.Session.LastRefreshedSessionWasExpired);

        StoreSession(h.Module, CreateToken(), DateTimeOffset.UtcNow.AddSeconds(-1));
        await h.Session.RefreshFromStorageAsync();
        Assert.True(h.Session.LastRefreshedSessionWasExpired);

        h.Module.Storage.Clear(); // 其它 tab 结束：读到 null，不是过期
        await h.Session.RefreshFromStorageAsync();
        Assert.False(h.Session.LastRefreshedSessionWasExpired);
    }

    // ------------------------------------------------------------ storage 订阅

    [Fact]
    public async Task Subscriptions_GetTheirOwnIds_AndUnsubscribingOneKeepsTheOther()
    {
        var h = new SessionHarness();

        var first = await h.Session.SubscribeStorageAsync(new object());
        var second = await h.Session.SubscribeStorageAsync(new object());

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first, second);
        Assert.Equal(new[] { first!.Value, second!.Value }, h.Module.ActiveSubscriptions);

        await h.Session.UnsubscribeStorageAsync(first.Value);

        Assert.Equal(new[] { second.Value }, h.Module.ActiveSubscriptions);
    }

    [Fact]
    public async Task Subscribe_ReturnsNullWhenTheScriptRefuses()
    {
        var h = new SessionHarness();
        h.Module.SubscribeException = new JSException("nope");

        Assert.Null(await h.Session.SubscribeStorageAsync(new object()));
    }
}
