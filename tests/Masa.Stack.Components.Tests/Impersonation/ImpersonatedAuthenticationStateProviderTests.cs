using System.Security.Claims;
using Masa.Stack.Components.Infrastructure.Impersonation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Xunit;
using static Masa.Stack.Components.Tests.Impersonation.ImpersonationTestData;

namespace Masa.Stack.Components.Tests.Impersonation;

public class ImpersonatedAuthenticationStateProviderTests
{
    private sealed class Fixture
    {
        public SessionHarness Harness { get; } = new();
        public FakeInnerProvider Inner { get; } = new();
        public ListLogger<ImpersonatedAuthenticationStateProvider> Log { get; } = new();
        public ImpersonatedAuthenticationStateProvider Provider { get; }
        public ImpersonationSession Session => Harness.Session;
        public FakeImpersonationModule Module => Harness.Module;

        public Fixture() => Provider = new ImpersonatedAuthenticationStateProvider(Inner, Session, Log);

        /// <summary>走完整的捕获流程建立一个有效的模拟会话。</summary>
        public async Task<string> StartImpersonationAsync(TokenSpec? spec = null)
        {
            var token = CreateToken(spec);
            Assert.True(await Harness.CaptureAsync(token));
            return token;
        }
    }

    // ------------------------------------------------------------ UI 身份

    [Fact]
    public async Task ActiveSession_RendersTheImpersonatedUser()
    {
        var f = new Fixture();
        f.Inner.User = FakeInnerProvider.Person("operator");
        await f.StartImpersonationAsync();

        var user = (await f.Provider.GetAuthenticationStateAsync()).User;

        Assert.True(user.Identity!.IsAuthenticated);
        Assert.Equal("impersonation", user.Identity.AuthenticationType);
        Assert.Equal("Target User", user.Identity.Name);
        Assert.Equal(TargetId, user.FindFirst("sub")!.Value);
        Assert.Equal("true", user.FindFirst(ImpersonatedAuthenticationStateProvider.ImpersonatedClaimType)!.Value);
        Assert.Equal(ActorId, user.FindFirst("actor_user_id")!.Value);

        // roles 的 JSON 数组被拆成独立的 role claim，AuthorizeView/IsInRole 才能正常工作
        Assert.True(user.IsInRole("admin"));
        Assert.True(user.IsInRole("auditor"));
        Assert.Equal(new[] { "admin", "auditor" }, user.FindAll("roles").Select(c => c.Value).OrderBy(v => v));
    }

    [Theory]
    [InlineData("admin", new[] { "admin" })]                         // 单值
    [InlineData("[\"a\",\"b\",\"\"]", new[] { "a", "b" })]            // 数组，忽略空项
    [InlineData("[oops", new[] { "[oops" })]                         // 非法 JSON 当单值
    public async Task Roles_AreParsedFromAnArrayOrASingleValue(string roles, string[] expected)
    {
        var f = new Fixture();
        await f.StartImpersonationAsync(new TokenSpec { Roles = roles });

        var user = (await f.Provider.GetAuthenticationStateAsync()).User;

        Assert.Equal(expected.OrderBy(v => v), user.FindAll("roles").Select(c => c.Value).OrderBy(v => v));
    }

    [Fact]
    public async Task WithoutASession_TheInnerIdentityPassesThrough()
    {
        var f = new Fixture();
        f.Inner.User = FakeInnerProvider.Person("alice");

        var user = (await f.Provider.GetAuthenticationStateAsync()).User;

        Assert.Equal("alice", user.Identity!.Name);
        Assert.Null(user.FindFirst(ImpersonatedAuthenticationStateProvider.ImpersonatedClaimType));
    }

    [Fact]
    public async Task GetOwnState_ExposesTheOperatorsIdentityWhileImpersonating()
    {
        // 横幅展示「发起人」需要拿到操作者本人的身份，而不能被模拟态遮住
        var f = new Fixture();
        f.Inner.User = FakeInnerProvider.PersonWithId(ActorId, "operator");
        await f.StartImpersonationAsync();

        var own = await f.Provider.GetOwnAuthenticationStateAsync();
        var impersonated = await f.Provider.GetAuthenticationStateAsync();

        Assert.Equal("operator", own.User.Identity!.Name);
        Assert.Equal("Target User", impersonated.User.Identity!.Name);
    }

    [Fact]
    public async Task AnExpiredStoredSession_FallsBackToTheInnerIdentityAndIsCleared()
    {
        var f = new Fixture();
        f.Inner.User = FakeInnerProvider.Person("alice");
        StoreSession(f.Module, CreateToken(), DateTimeOffset.UtcNow.AddSeconds(-5));

        var user = (await f.Provider.GetAuthenticationStateAsync()).User;

        Assert.Equal("alice", user.Identity!.Name);
        Assert.Empty(f.Module.Storage);
    }

    [Fact]
    public async Task ACorruptedStoredToken_FallsBackToTheInnerIdentityAndEndsTheSession()
    {
        // 绝不放行一个解析不了的 token 对应的「身份」
        var f = new Fixture();
        f.Inner.User = FakeInnerProvider.Person("alice");
        StoreSession(f.Module, "garbage", DateTimeOffset.UtcNow.AddMinutes(5));

        var user = (await f.Provider.GetAuthenticationStateAsync()).User;

        Assert.Equal("alice", user.Identity!.Name);
        Assert.False(f.Session.IsActive);
        Assert.Empty(f.Module.Storage);
    }

    // ------------------------------------------------------------ 状态变化联动

    [Fact]
    public async Task SigningOutOfTheOwnSession_EndsTheImpersonation()
    {
        var f = new Fixture();
        f.Inner.User = FakeInnerProvider.Person("operator");
        await f.StartImpersonationAsync();

        f.Inner.RaiseStateChanged(FakeInnerProvider.Anonymous);

        await Wait.UntilAsync(() => !f.Session.IsActive);
        Assert.Empty(f.Module.Storage); // 下次登录后不能残留模拟态
    }

    [Fact]
    public async Task InnerStateChange_WhileImpersonating_PublishesTheImpersonatedIdentityNotTheInnerOne()
    {
        var f = new Fixture();
        await f.StartImpersonationAsync();
        var published = new TaskCompletionSource<Task<AuthenticationState>>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Provider.AuthenticationStateChanged += task => published.TrySetResult(task);

        // OIDC 续期：本人仍然登录着，只是触发了状态变化
        f.Inner.RaiseStateChanged(FakeInnerProvider.Person("operator"));

        var state = await await published.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal("Target User", state.User.Identity!.Name);
        Assert.True(f.Session.IsActive);
    }

    [Fact]
    public async Task SessionChange_PublishesTheNewIdentity()
    {
        var f = new Fixture();
        f.Inner.User = FakeInnerProvider.Person("operator");
        await f.StartImpersonationAsync();
        var published = new TaskCompletionSource<Task<AuthenticationState>>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Provider.AuthenticationStateChanged += task => published.TrySetResult(task);

        await f.Provider.EndImpersonationAsync();

        var state = await await published.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal("operator", state.User.Identity!.Name);
    }

    // ------------------------------------------------------------ API 身份（Bearer）

    [Fact]
    public async Task RequestAccessToken_ReturnsTheImpersonationTokenWhileActive()
    {
        var f = new Fixture();
        var token = await f.StartImpersonationAsync();

        var result = await f.Provider.RequestAccessToken();

        Assert.Equal(AccessTokenResultStatus.Success, result.Status);
        Assert.True(result.TryGetToken(out var accessToken));
        Assert.Equal(token, accessToken.Value);
        Assert.Equal(0, f.Inner.AccessTokenRequests);
    }

    [Fact]
    public async Task RequestAccessToken_FallsBackToTheInnerProviderWithoutASession()
    {
        var f = new Fixture();

        var result = await f.Provider.RequestAccessToken();

        Assert.True(result.TryGetToken(out var accessToken));
        Assert.Equal("inner-token", accessToken.Value);
        Assert.Equal(1, f.Inner.AccessTokenRequests);
    }

    [Fact]
    public async Task RequestAccessToken_StopsUsingTheImpersonationTokenOnceItExpires()
    {
        var f = new Fixture();
        StoreSession(f.Module, CreateToken(), DateTimeOffset.UtcNow.AddSeconds(-1));

        var result = await f.Provider.RequestAccessToken();

        Assert.True(result.TryGetToken(out var accessToken));
        Assert.Equal("inner-token", accessToken.Value);
        Assert.Empty(f.Module.Storage);
    }

    [Fact]
    public async Task RequestAccessTokenWithScopes_ReturnsTheImpersonationTokenEvenForScopesItDoesNotCarry()
    {
        // 框架里 RequiresRedirect 会被 AccessTokenNotAvailableException.Redirect() 导航到 InteractiveRequestUrl；
        // 模拟态下没有合法的交互地址（空串会把用户带到站点根），所以不能返回它——由 API 以 403 给出权威答案。
        var f = new Fixture();
        var token = await f.StartImpersonationAsync();

        var result = await f.Provider.RequestAccessToken(new AccessTokenRequestOptions { Scopes = new[] { "masa.api" } });

        Assert.Equal(AccessTokenResultStatus.Success, result.Status);
        Assert.True(result.TryGetToken(out var accessToken));
        Assert.Equal(token, accessToken.Value);
        Assert.Equal(new[] { "openid", "profile" }, accessToken.GrantedScopes); // 如实声明实际拥有的 scope
    }

    [Fact]
    public async Task RequestAccessTokenWithScopes_WarnsOncePerMissingScopeOnly()
    {
        var f = new Fixture();
        await f.StartImpersonationAsync();
        var options = new AccessTokenRequestOptions { Scopes = new[] { "openid", "masa.api" } };

        await f.Provider.RequestAccessToken(options);
        await f.Provider.RequestAccessToken(options);
        await f.Provider.RequestAccessToken(new AccessTokenRequestOptions { Scopes = new[] { "masa.other" } });

        var warnings = f.Log.Warnings.ToList();
        Assert.Equal(2, warnings.Count);
        Assert.Single(warnings, w => w.Contains("masa.api"));
        Assert.Single(warnings, w => w.Contains("masa.other"));
        Assert.DoesNotContain(warnings, w => w.Contains("'openid'")); // 已授予的 scope 不告警
    }

    [Fact]
    public async Task RequestAccessTokenWithScopes_FallsBackToTheInnerProviderWithoutASession()
    {
        var f = new Fixture();

        var result = await f.Provider.RequestAccessToken(new AccessTokenRequestOptions { Scopes = new[] { "masa.api" } });

        Assert.True(result.TryGetToken(out var accessToken));
        Assert.Equal("inner-token", accessToken.Value);
        Assert.Empty(f.Log.Warnings);
    }

    // ------------------------------------------------------------ 登录/登出导航层

    [Fact]
    public async Task RemoteAuthenticationOperations_AreForwardedToTheInnerService()
    {
        var f = new Fixture();
        var context = new RemoteAuthenticationContext<RemoteAuthenticationState>();

        Assert.Equal(RemoteAuthenticationStatus.Success, (await f.Provider.SignInAsync(context)).Status);
        Assert.Equal(RemoteAuthenticationStatus.Success, (await f.Provider.CompleteSignInAsync(context)).Status);
        Assert.Equal(RemoteAuthenticationStatus.Success, (await f.Provider.SignOutAsync(context)).Status);
        Assert.Equal(RemoteAuthenticationStatus.Success, (await f.Provider.CompleteSignOutAsync(context)).Status);
    }
}
