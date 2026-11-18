using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Masa.Stack.Components.Extensions.OpenIdConnect;
using Masa.Stack.Components.Infrastructure.Impersonation;
using Masa.Stack.Components.Layouts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using Xunit;

namespace Masa.Stack.Components.Tests.Impersonation;

/// <summary>描述一个要伪造的模拟 token。默认值就是 SSO 会签发的、能通过所有检查的 token。</summary>
internal sealed record TokenSpec
{
    public string Issuer { get; init; } = ImpersonationTestData.Issuer;
    public string? Sub { get; init; } = ImpersonationTestData.TargetId;
    public bool IncludeAct { get; init; } = true;
    public string? ActSub { get; init; } = ImpersonationTestData.ActorId;

    /// <summary>非 null 时原样作为 act claim 的 JSON（用来构造畸形 act）。</summary>
    public string? ActRaw { get; init; }

    /// <summary>act claim 里的 userName 附加属性（RFC 8693 允许；发送端可选提供）。null 表示不提供。</summary>
    public string? ActUserName { get; init; }

    public string Scope { get; init; } = "openid profile";
    public string Roles { get; init; } = "[\"admin\",\"auditor\"]";
    public string UserName { get; init; } = "Target User";
    public DateTime? NotBefore { get; init; } = DateTime.UtcNow.AddMinutes(-1);
    public DateTime? Expires { get; init; } = DateTime.UtcNow.AddMinutes(10);
}

internal static class ImpersonationTestData
{
    public const string Issuer = "https://sso.test";
    public const string Authority = "https://sso.test";
    public const string TargetId = "11111111-1111-1111-1111-111111111111";
    public const string ActorId = "22222222-2222-2222-2222-222222222222";

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 用一把随机的 HMAC 密钥签发 token。接收端不验签（只有 SSO 验得了），所以签名密钥无关紧要，
    /// 这里也正好模拟「攻击者自己签的 token」。
    /// </summary>
    public static string CreateToken(TokenSpec? spec = null)
    {
        spec ??= new TokenSpec();

        var claims = new List<Claim>
        {
            new("userName", spec.UserName),
            new("roles", spec.Roles),
        };
        if (spec.Sub is not null)
        {
            claims.Add(new Claim("sub", spec.Sub));
        }
        if (spec.ActRaw is not null)
        {
            claims.Add(new Claim("act", spec.ActRaw, JsonClaimValueTypes.Json));
        }
        else if (spec.IncludeAct)
        {
            var act = spec.ActUserName is null
                ? JsonSerializer.Serialize(new { sub = spec.ActSub })
                : JsonSerializer.Serialize(new { sub = spec.ActSub, userName = spec.ActUserName });
            claims.Add(new Claim("act", act, JsonClaimValueTypes.Json));
        }
        foreach (var scope in spec.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            claims.Add(new Claim("scope", scope));
        }

        var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
        var token = new JwtSecurityToken(
            issuer: spec.Issuer,
            audience: null,
            claims: claims,
            notBefore: spec.NotBefore,
            expires: spec.Expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>JS captureFragment 的返回值形状。</summary>
    public static object Fragment(string accessToken, int expiresIn = 900) => new { accessToken, expiresIn };

    /// <summary>把一个会话直接写进假 localStorage（模拟「另一个 tab / 上一次访问」留下的状态）。</summary>
    public static void StoreSession(
        FakeImpersonationModule module,
        string accessToken,
        DateTimeOffset expiresAt,
        string targetUserId = TargetId,
        string actorUserId = ActorId,
        string targetUserName = "Target User",
        string actorUserName = "")
    {
        var item = new ImpersonationSession.ImpersonationSessionItem
        {
            AccessToken = accessToken,
            ExpiresAtUnixSeconds = expiresAt.ToUnixTimeSeconds(),
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            GrantedScopes = new List<string> { "openid", "profile" },
        };
        module.Storage[ImpersonationSession.StorageKey] = JsonSerializer.Serialize(item, Web);
    }
}

/// <summary>装配好的会话 + 它的假 JS + 日志。</summary>
internal sealed class SessionHarness
{
    public FakeJsRuntime Js { get; } = new();
    public FakeImpersonationModule Module => Js.Module;
    public ListLogger<ImpersonationSession> Log { get; } = new();
    public ImpersonationSession Session { get; }

    public SessionHarness(MasaOpenIdConnectOptions? options = null, bool withoutOptions = false)
    {
        options ??= new MasaOpenIdConnectOptions { Authority = ImpersonationTestData.Authority };
        Session = new ImpersonationSession(Js, Log, withoutOptions ? null : options);
    }

    /// <summary>让 captureFragment 返回给定 token，并执行一次捕获。</summary>
    public Task<bool> CaptureAsync(string token, int expiresIn = 900)
    {
        Module.Fragment = ImpersonationTestData.Fragment(token, expiresIn);
        return Session.CaptureFromUrlAsync();
    }
}

internal sealed class FakeJsRuntime : IJSRuntime
{
    public FakeImpersonationModule Module { get; } = new();
    public int ImportCount { get; private set; }

    /// <summary>非 null 时 import 失败（模拟脚本 404）。</summary>
    public Exception? ImportFailure { get; set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (identifier != "import")
        {
            throw new NotSupportedException($"FakeJsRuntime does not support '{identifier}'.");
        }

        ImportCount++;
        return ImportFailure is not null
            ? ValueTask.FromException<TValue>(ImportFailure)
            : new ValueTask<TValue>((TValue)(object)Module);
    }
}

/// <summary>impersonation.js 的内存替身：把 JS 侧的各个导出函数按它们的契约模拟出来。</summary>
internal sealed class FakeImpersonationModule : IJSObjectReference
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private int _nextSubscriptionId = 1;

    public Dictionary<string, string> Storage { get; } = new();

    /// <summary>captureFragment 的返回值；null 表示地址栏里没有模拟 token。</summary>
    public object? Fragment { get; set; }

    public int ProbeStatus { get; set; } = 200;
    public string ProbeSub { get; set; } = ImpersonationTestData.TargetId;
    public Exception? ProbeException { get; set; }
    public Exception? SubscribeException { get; set; }

    public List<(string Endpoint, string Token, int TimeoutMs)> Probes { get; } = new();
    public List<int> ActiveSubscriptions { get; } = new();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        args ??= Array.Empty<object?>();
        switch (identifier)
        {
            case "captureFragment":
                return Result<TValue>(Fragment);

            case "probeUserInfo":
                Probes.Add(((string)args[0]!, (string)args[1]!, (int)args[2]!));
                if (ProbeException is not null)
                {
                    return ValueTask.FromException<TValue>(ProbeException);
                }
                return Result<TValue>(new { status = ProbeStatus, sub = ProbeSub });

            case "readSession":
                return Storage.TryGetValue((string)args[0]!, out var json)
                    ? new ValueTask<TValue>(JsonSerializer.Deserialize<TValue>(json, Web)!)
                    : default;

            case "writeSession":
                Storage[(string)args[0]!] = JsonSerializer.Serialize(args[1], Web);
                return default;

            case "clearSession":
                Storage.Remove((string)args[0]!);
                return default;

            case "subscribeStorage":
                if (SubscribeException is not null)
                {
                    return ValueTask.FromException<TValue>(SubscribeException);
                }
                var id = _nextSubscriptionId++;
                ActiveSubscriptions.Add(id);
                return Result<TValue>(id);

            case "unsubscribeStorage":
                ActiveSubscriptions.Remove((int)args[0]!);
                return default;

            default:
                throw new NotSupportedException($"FakeImpersonationModule does not support '{identifier}'.");
        }
    }

    private static ValueTask<TValue> Result<TValue>(object? value)
        => value is null
            ? default
            : new ValueTask<TValue>(JsonSerializer.Deserialize<TValue>(JsonSerializer.Serialize(value, Web), Web)!);

    public ValueTask DisposeAsync() => default;
}

internal sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = new();

    public IEnumerable<string> Warnings => Entries.Where(e => e.Level == LogLevel.Warning).Select(e => e.Message);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception)));
}

/// <summary>
/// 代表「本人 OIDC」的内层 provider。装饰器要求内层同时实现三个接口（框架的 RemoteAuthenticationService 就是这样），
/// 这里全部实现，并记录调用，方便断言「透传」。
/// </summary>
internal sealed class FakeInnerProvider
    : AuthenticationStateProvider, IAccessTokenProvider, IRemoteAuthenticationService<RemoteAuthenticationState>
{
    public static ClaimsPrincipal Anonymous => new(new ClaimsIdentity());

    public static ClaimsPrincipal Person(string name)
        => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, name) }, "oidc"));

    /// <summary>带 sub claim 的本人身份（横幅「发起人」回退解析与 act.sub 比对用）。</summary>
    public static ClaimsPrincipal PersonWithId(string userId, string name)
        => new(new ClaimsIdentity(new[] { new Claim("sub", userId), new Claim(ClaimTypes.Name, name) }, "oidc"));

    public ClaimsPrincipal User { get; set; } = Anonymous;
    public int AccessTokenRequests { get; private set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(User));

    /// <summary>模拟本人 OIDC 会话状态变化（续期 / 登出）。</summary>
    public void RaiseStateChanged(ClaimsPrincipal user)
    {
        User = user;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public ValueTask<AccessTokenResult> RequestAccessToken()
    {
        AccessTokenRequests++;
        return new ValueTask<AccessTokenResult>(InnerResult());
    }

    public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
    {
        AccessTokenRequests++;
        return new ValueTask<AccessTokenResult>(InnerResult());
    }

    private static AccessTokenResult InnerResult() => new(
        AccessTokenResultStatus.Success,
        new AccessToken { Value = "inner-token", Expires = DateTimeOffset.Now.AddMinutes(5) },
        string.Empty,
        null);

    public Task<RemoteAuthenticationResult<RemoteAuthenticationState>> SignInAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context) => Ok();
    public Task<RemoteAuthenticationResult<RemoteAuthenticationState>> CompleteSignInAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context) => Ok();
    public Task<RemoteAuthenticationResult<RemoteAuthenticationState>> SignOutAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context) => Ok();
    public Task<RemoteAuthenticationResult<RemoteAuthenticationState>> CompleteSignOutAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context) => Ok();

    private static Task<RemoteAuthenticationResult<RemoteAuthenticationState>> Ok()
        => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState> { Status = RemoteAuthenticationStatus.Success });
}

internal static class Wait
{
    /// <summary>OnInnerStateChanged 是 async void，只能轮询它的效果。</summary>
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

/// <summary>横幅状态机测试用的 UI 替身：记录提示与刷新，跨线程安全（真实 Timer 的首个 tick 在线程池线程上跑）。</summary>
internal sealed class RecordingUi
{
    private readonly object _gate = new();
    private int _reloads;
    private readonly List<string> _warnings = new();

    public Task ShowWarning(string message)
    {
        lock (_gate)
        {
            _warnings.Add(message);
        }
        return Task.CompletedTask;
    }

    public Task Reload()
    {
        Interlocked.Increment(ref _reloads);
        return Task.CompletedTask;
    }

    public int Reloads => Volatile.Read(ref _reloads);

    /// <summary>提示文案（即翻译后的 key）的快照。</summary>
    public IReadOnlyList<string> WarningsSnapshot()
    {
        lock (_gate)
        {
            return _warnings.ToList();
        }
    }
}

/// <summary>
/// 装配好的 <see cref="ImpersonationBannerController"/>：内联加锁派发（模拟组件的 InvokeAsync）、
/// 即时 delay、可记录的提示与刷新。TickInterval 拉长到不干扰测试，倒计时由测试直接调 TickAsync 驱动。
/// </summary>
internal sealed class BannerHarness
{
    private readonly object _gate = new();
    private readonly AuthenticationStateProvider? _provider;

    public SessionHarness SessionHarness { get; } = new();
    public FakeInnerProvider Inner { get; } = new();
    public ImpersonatedAuthenticationStateProvider? Decorator { get; }
    public RecordingUi Ui { get; } = new();
    public ListLogger<ImpersonationBannerController> Log { get; } = new();
    public ImpersonationBannerController Controller { get; }
    public ImpersonationSession Session => SessionHarness.Session;

    /// <param name="provider">
    /// 横幅可解析到的 AuthenticationStateProvider。null 且 <paramref name="registerDecorator"/> 为 false 时
    /// 模拟「宿主未注册该服务」；传入非装饰器的 provider 时模拟「自建认证装配」。
    /// </param>
    public BannerHarness(AuthenticationStateProvider? provider = null, bool registerDecorator = true)
    {
        if (registerDecorator)
        {
            Decorator = new ImpersonatedAuthenticationStateProvider(Inner, Session, null);
        }
        _provider = provider ?? (AuthenticationStateProvider?)Decorator;

        Controller = new ImpersonationBannerController(
            Session,
            () => _provider,
            key => key,
            Ui.ShowWarning,
            Ui.Reload,
            Dispatch,
            _ => Task.CompletedTask,
            () => new object(),
            Log)
        {
            TickInterval = TimeSpan.FromHours(1),
        };
    }

    /// <summary>先完整捕获一个有效模拟会话，再初始化横幅（最常见的前置状态）。</summary>
    public async Task CaptureAndInitializeAsync(TokenSpec? spec = null)
    {
        await CaptureAsync(spec);
        await Controller.InitializeAsync();
    }

    public async Task CaptureAsync(TokenSpec? spec = null)
    {
        var token = ImpersonationTestData.CreateToken(spec);
        Assert.True(await SessionHarness.CaptureAsync(token));
    }

    // 内联派发：与组件的 InvokeAsync 语义一致（在调用线程上执行），Monitor 同线程可重入，
    // 锁只用来与真实 Timer 的首个 tick（线程池线程）互斥。
    private Task Dispatch(Func<Task> operation)
    {
        lock (_gate)
        {
            return operation();
        }
    }
}
