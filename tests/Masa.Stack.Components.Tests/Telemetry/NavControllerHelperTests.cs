using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Masa.BuildingBlocks.StackSdks.Config.Consts;
using Masa.Stack.Components.OpenTelemetry.Blazor;
using Masa.Stack.Components.OpenTelemetry.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Masa.Stack.Components.Tests.Telemetry;

/// <summary>
/// 模拟登录入口 URL 形如 <c>/pm/projects#access_token=...&amp;expires_in=900</c>。
/// Blazor WASM 的 <see cref="NavigationManager.Uri"/> 在 <c>WebAssemblyHostBuilder</c> 构造时就缓存了 location.href，
/// 接收端清理地址栏后它依然带着 token，所以遥测在写 span 标签 / 日志作用域前必须先剥离 fragment。
/// 这些用例锁住这个约束，防止短命 access_token 被导出到 OTLP 后端。
/// </summary>
// 这些用例会读写 MasaBlazorActivityContent / RouteUtils 的静态状态，类内顺序执行，不要和其它改这些静态的类并行。
public class NavControllerHelperTests
{
    private const string BaseUri = "https://portal.test/";
    private const string Secret = "SECRET.JWT.VALUE";

    private static string FullUri(string pathAndQuery = "pm/projects?tab=1")
        => $"{BaseUri}{pathAndQuery}#access_token={Secret}&expires_in=900";

    [Theory]
    [InlineData("https://app.test/pm/projects", "https://app.test/pm/projects")]
    [InlineData("https://app.test/pm/projects?tab=1", "https://app.test/pm/projects?tab=1")]
    [InlineData("https://app.test/pm/projects#access_token=SECRET&expires_in=900", "https://app.test/pm/projects")]
    [InlineData("https://app.test/pm/projects?tab=1#access_token=SECRET", "https://app.test/pm/projects?tab=1")]
    [InlineData("https://app.test/#", "https://app.test/")]
    [InlineData("", "")]
    public void RemoveFragment_StripsEverythingFromTheHash(string input, string expected)
    {
        Assert.Equal(expected, RouteUtils.RemoveFragment(input));
    }

    [Fact]
    public void StartPageActivity_DoesNotLeakTheFragmentIntoAnyTag()
    {
        var page = new object();
        const string sourceName = "NavControllerHelperTests.NoLeak";
        using var source = new ActivitySource(sourceName);
        using var listener = Listen(sourceName);
        ResetStatics();

        Activity? activity = null;
        try
        {
            activity = NavControllerHelper.StartPageActivity(
                source, page, new FakeNavigationManager(BaseUri, FullUri()), "/", "test-agent");

            Assert.NotNull(activity);

            // 任何标签、显示名里都不能出现 token 或 fragment 标记
            var everything = activity!.TagObjects
                .Select(t => $"{t.Key}={t.Value}")
                .Append(activity.DisplayName)
                .ToList();
            Assert.DoesNotContain(everything, s => s.Contains(Secret, StringComparison.Ordinal));
            Assert.DoesNotContain(everything, s => s.Contains("access_token", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(everything, s => s.Contains('#'));

            // 行为保持不变：路径和查询串仍然完整保留
            Assert.Equal("https://portal.test/pm/projects?tab=1", activity.GetTagItem(MasaBlazorWasmConstants.HttpRequestUrlFull));
            Assert.Equal("/pm/projects?tab=1", activity.GetTagItem(MasaBlazorWasmConstants.BlazorPagePath));

            // 页面注册表用同一个（已净化的）url 作 key，EndPageActivity 才能按标签值把它移除
            Assert.Same(page, MasaBlazorActivityContent.GetPage("/pm/projects?tab=1"));
        }
        finally
        {
            activity?.Stop();
            ResetStatics();
        }
    }

    [Fact]
    public void LogScope_DoesNotContainTheFragment()
    {
        // BlazorPagePath 标签会被 GetLogScopeValues 原样写进日志作用域，所以日志同样不能带 token
        const string sourceName = "NavControllerHelperTests.LogScope";
        using var source = new ActivitySource(sourceName);
        using var listener = Listen(sourceName);
        ResetStatics();

        Activity? activity = null;
        try
        {
            var page = new MasaBlazorOpenTelemetryBasePage();
            activity = NavControllerHelper.StartPageActivity(
                source, page, new FakeNavigationManager(BaseUri, FullUri()), "/", "test-agent");
            Assert.NotNull(activity);
            page.Activity = activity;

            var scope = page.GetLogScopeValues();

            Assert.Equal("/pm/projects?tab=1", scope[MasaBlazorWasmConstants.BlazorPagePath]);
            Assert.DoesNotContain(scope.Values, v => v.ToString()!.Contains(Secret, StringComparison.Ordinal));
        }
        finally
        {
            activity?.Stop();
            ResetStatics();
        }
    }

    [Fact]
    public void IsPage_MatchesMultiRoutePagesEvenWhenTheUriCarriesAFragment()
    {
        // 多路由页面按路径逐段匹配；fragment 若不剥离，末段会变成 "projects#access_token=..." 而匹配失败，
        // 导致入口页没有任何页面 span。
        RouteUtils._routes[typeof(MultiRoutePage)] = ["/pm/projects", "/pm/projects/{id}"];
        try
        {
            var navigation = new FakeNavigationManager(BaseUri, FullUri("pm/projects"));

            var isPage = RouteUtils.IsPage(typeof(MultiRoutePage), navigation, "/", out var template);

            Assert.True(isPage);
            Assert.Equal("/pm/projects", template);
        }
        finally
        {
            RouteUtils._routes.Remove(typeof(MultiRoutePage));
        }
    }

    private static ActivityListener Listen(string sourceName)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static void ResetStatics()
    {
        MasaBlazorActivityContent.CurrentActivity = null;
        MasaBlazorActivityContent.BlazorPage = null;
        MasaBlazorActivityContent.RemovePage("/pm/projects?tab=1");
        MasaBlazorActivityContent.RemovePage("/pm/projects");
        NavControllerHelper.RefreshCurrentUserId(null, null, null);
        Activity.Current = null;
    }

    private sealed class MultiRoutePage;

    private sealed class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager(string baseUri, string uri) => Initialize(baseUri, uri);

        protected override void NavigateToCore(string uri, NavigationOptions options)
            => throw new NotSupportedException();
    }

    // ------------------------------------------------------------ 模拟登录审计归因（act → enduser.actor.*）

    /// <summary>构造一个「访问令牌」：模拟 token 带 act（sub=操作者、userName=操作者姓名），普通 token 不带。</summary>
    private static string CreateIdentityToken(bool impersonated, bool malformedAct = false)
    {
        var claims = new List<Claim>
        {
            new(IdentityClaimConsts.USER_ID, "target-user"),
            new(IdentityClaimConsts.USER_NAME, "Target User"),
        };
        if (impersonated)
        {
            // 注意：畸形 act 不能用 JsonClaimValueTypes（WriteToken 无法把非法 JSON 序列化为 raw claim），
            // 按普通字符串 claim 写入即可——读取端照样会尝试 JSON 解析并失败
            var act = malformedAct
                ? "{oops"
                : JsonSerializer.Serialize(new { sub = "operator-user", userName = "Operator One" });
            claims.Add(new Claim("act", act));
        }

        var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: "https://sso.test",
            audience: null,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
    }

    private static (Activity Activity, IDisposable Listener) StartBareActivity(string sourceName)
    {
        var source = new ActivitySource(sourceName);
        var listener = Listen(sourceName);
        var activity = source.StartActivity("test");
        Assert.NotNull(activity);
        return (activity!, listener);
    }

    [Fact]
    public void SetUserIdentity_OnAnImpersonationToken_RecordsBothTheTargetAndTheActor()
    {
        // 模拟期间 enduser.* 如实显示被模拟用户，但操作者必须同时落进 enduser.actor.*，
        // 否则模拟窗口内的页面活动全部归因到被模拟用户，「代操作」无从审计
        const string sourceName = "NavControllerHelperTests.Actor";
        ResetStatics();
        var (activity, listener) = StartBareActivity(sourceName);
        try
        {
            Assert.True(NavControllerHelper.SetUserIdentity(activity, CreateIdentityToken(impersonated: true)));

            Assert.Equal("target-user", activity.GetTagItem(MasaBlazorWasmConstants.SessionUserId));
            Assert.Equal("Target User", activity.GetTagItem(MasaBlazorWasmConstants.SessionUserName));
            Assert.Equal("operator-user", activity.GetTagItem(MasaBlazorWasmConstants.SessionActorUserId));
            Assert.Equal("Operator One", activity.GetTagItem(MasaBlazorWasmConstants.SessionActorUserName));
            Assert.NotNull(activity.GetTagItem(MasaBlazorWasmConstants.BlazorPageSessionId));
        }
        finally
        {
            activity.Stop();
            listener.Dispose();
            ResetStatics();
        }
    }

    [Fact]
    public void SetUserIdentity_OnAnOrdinaryToken_RecordsNoActor()
    {
        const string sourceName = "NavControllerHelperTests.NoActor";
        ResetStatics();
        var (activity, listener) = StartBareActivity(sourceName);
        try
        {
            Assert.True(NavControllerHelper.SetUserIdentity(activity, CreateIdentityToken(impersonated: false)));

            Assert.Equal("target-user", activity.GetTagItem(MasaBlazorWasmConstants.SessionUserId));
            Assert.Null(activity.GetTagItem(MasaBlazorWasmConstants.SessionActorUserId));
            Assert.Null(activity.GetTagItem(MasaBlazorWasmConstants.SessionActorUserName));
        }
        finally
        {
            activity.Stop();
            listener.Dispose();
            ResetStatics();
        }
    }

    [Fact]
    public void SetUserIdentity_ToleratesAMalformedActClaim()
    {
        const string sourceName = "NavControllerHelperTests.BadAct";
        ResetStatics();
        var (activity, listener) = StartBareActivity(sourceName);
        try
        {
            Assert.True(NavControllerHelper.SetUserIdentity(activity, CreateIdentityToken(impersonated: true, malformedAct: true)));

            // act 畸形：操作者拿不到，但用户自己的标签仍然完整
            Assert.Equal("target-user", activity.GetTagItem(MasaBlazorWasmConstants.SessionUserId));
            Assert.Null(activity.GetTagItem(MasaBlazorWasmConstants.SessionActorUserId));
        }
        finally
        {
            activity.Stop();
            listener.Dispose();
            ResetStatics();
        }
    }

    [Fact]
    public void SetUserIdentity_WithNoToken_ChangesNothing()
    {
        const string sourceName = "NavControllerHelperTests.NoToken";
        ResetStatics();
        var (activity, listener) = StartBareActivity(sourceName);
        try
        {
            Assert.False(NavControllerHelper.SetUserIdentity(activity, null));
            Assert.False(NavControllerHelper.SetUserIdentity(activity, "not-a-jwt"));

            Assert.Null(activity.GetTagItem(MasaBlazorWasmConstants.SessionUserId));
            Assert.Null(activity.GetTagItem(MasaBlazorWasmConstants.SessionActorUserId));
        }
        finally
        {
            activity.Stop();
            listener.Dispose();
            ResetStatics();
        }
    }

    [Fact]
    public void StartPageActivity_PropagatesTheActorToLaterActivities()
    {
        // 静态快照传播：首个活动带上了操作者，之后 StartPageActivity 的新活动也必须带
        const string sourceName = "NavControllerHelperTests.Propagate";
        using var source = new ActivitySource(sourceName);
        using var listener = Listen(sourceName);
        ResetStatics();

        Activity? first = null;
        Activity? second = null;
        try
        {
            first = source.StartActivity("first");
            Assert.NotNull(first);
            Assert.True(NavControllerHelper.SetUserIdentity(first, CreateIdentityToken(impersonated: true)));

            second = NavControllerHelper.StartPageActivity(
                source, new MasaBlazorOpenTelemetryBasePage(), new FakeNavigationManager(BaseUri, FullUri()), "/", "test-agent");
            Assert.NotNull(second);

            Assert.Equal("target-user", second!.GetTagItem(MasaBlazorWasmConstants.SessionUserId));
            Assert.Equal("operator-user", second.GetTagItem(MasaBlazorWasmConstants.SessionActorUserId));
            Assert.Equal("Operator One", second.GetTagItem(MasaBlazorWasmConstants.SessionActorUserName));
        }
        finally
        {
            second?.Stop();
            first?.Stop();
            ResetStatics();
        }
    }

    [Fact]
    public void LogScope_ContainsTheActorIdDuringImpersonation()
    {
        // 日志作用域与 span 标签同样要能归因操作者
        const string sourceName = "NavControllerHelperTests.ActorScope";
        using var source = new ActivitySource(sourceName);
        using var listener = Listen(sourceName);
        ResetStatics();

        Activity? activity = null;
        try
        {
            var page = new MasaBlazorOpenTelemetryBasePage();
            activity = NavControllerHelper.StartPageActivity(
                source, page, new FakeNavigationManager(BaseUri, FullUri()), "/", "test-agent");
            Assert.NotNull(activity);
            page.Activity = activity;

            Assert.True(NavControllerHelper.SetUserIdentity(activity, CreateIdentityToken(impersonated: true)));

            var scope = page.GetLogScopeValues();
            Assert.Equal("operator-user", scope[MasaBlazorWasmConstants.SessionActorUserId]);
            Assert.DoesNotContain(scope.Values, v => v.ToString()!.Contains("Operator One", StringComparison.Ordinal));
        }
        finally
        {
            activity?.Stop();
            ResetStatics();
        }
    }
}
