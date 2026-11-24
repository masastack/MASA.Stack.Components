using System.Globalization;
using System.Net;
using Masa.Blazor;
using Masa.Contrib.Authentication.Identity.BlazorWebAssembly;
using Masa.Contrib.Authentication.Identity.Core;
using Masa.Stack.Components.Infrastructure.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Xunit;

namespace Masa.Stack.Components.Tests.Http;

/// <summary>
/// <see cref="I18n"/> 的构造 / <see cref="I18n.SetCulture"/> 会改写进程级的
/// <see cref="CultureInfo.DefaultThreadCurrentCulture"/> 与 <see cref="CultureInfo.DefaultThreadCurrentUICulture"/>，
/// 所以这些测试不能和别的测试并行跑，并且每个测试结束都要把它们还原。
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CultureStateCollection
{
    public const string Name = "Process-wide culture state";
}

/// <summary>
/// 回归：MFE 发出的 Accept-Language 恒为 <c>en-US, en; q=0.9</c>，与界面语言无关。
/// <para>
/// 根因是 handler 在 <see cref="IHttpClientFactory"/> 自建的 DI scope 里构造，注入到的 <see cref="I18n"/>
/// 是一个默认 en-US 的新实例，而不是界面（<c>GlobalConfig.Culture</c> 切换语言时修改的）那一个。
/// 这里完全按生产的结构搭环境：根容器 + 真实的 <see cref="IHttpClientFactory"/> +
/// 一个「界面 scope」（等同 WebAssemblyHost.Services）+ 真实的 accessor，请求最终落到一个记录请求头的主 handler 上。
/// </para>
/// </summary>
[Collection(CultureStateCollection.Name)]
public sealed class AcceptLanguageHandlerTests : IDisposable
{
    private const string Chinese = "zh-CN, zh; q=0.9, en; q=0.8";
    private const string English = "en-US, en; q=0.9";

    private static readonly CultureInfo EnUs = new("en-US");
    private static readonly CultureInfo ZhCn = new("zh-CN");

    private readonly CultureInfo? _originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
    private readonly CultureInfo? _originalDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
    private readonly CultureInfo _originalCurrentCulture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _originalCurrentUiCulture = CultureInfo.CurrentUICulture;

    public void Dispose()
    {
        CultureInfo.DefaultThreadCurrentCulture = _originalDefaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _originalDefaultUiCulture;
        CultureInfo.CurrentCulture = _originalCurrentCulture;
        CultureInfo.CurrentUICulture = _originalCurrentUiCulture;
    }

    // ------------------------------------------------------------ 跟随界面语言

    [Theory]
    [InlineData("zh-CN", Chinese)]
    [InlineData("en-US", English)]
    public async Task Header_FollowsTheCultureOfTheUiI18n(string uiCulture, string expected)
    {
        using var fixture = new Fixture();
        var i18n = fixture.StartUiScope();

        // 与 GlobalConfig.Culture 的 setter 完全一致：_i18N.SetCulture(new CultureInfo("en-US"), value)
        i18n.SetCulture(EnUs, new CultureInfo(uiCulture));

        Assert.Equal(expected, await fixture.SendAsync());
    }

    [Fact]
    public async Task Header_TracksLanguageSwitches_OnTheSameHttpClient()
    {
        using var fixture = new Fixture();
        var i18n = fixture.StartUiScope();
        using var client = fixture.CreateClient();

        // handler 随管线被 IHttpClientFactory 缓存，所以语言必须在每次发请求时现读，而不是在构造时固定
        i18n.SetCulture(EnUs, ZhCn);
        Assert.Equal(Chinese, await fixture.SendAsync(client));

        i18n.SetCulture(EnUs, EnUs);
        Assert.Equal(English, await fixture.SendAsync(client));

        i18n.SetCulture(EnUs, ZhCn);
        Assert.Equal(Chinese, await fixture.SendAsync(client));
    }

    [Fact]
    public async Task EveryNamedClient_GetsTheHeader()
    {
        using var fixture = new Fixture();
        fixture.StartUiScope().SetCulture(EnUs, ZhCn);

        // 如果 handler 被注册成 Singleton，第二个命名客户端构建管线时就会抛
        // "'DelegatingHandler' instances provided to 'HttpMessageHandlerBuilder' must not be reused or cached"
        Assert.Equal(Chinese, await fixture.SendAsync(Fixture.ClientA));
        Assert.Equal(Chinese, await fixture.SendAsync(Fixture.ClientB));
    }

    // ------------------------------------------------------------ 不再制造第二个 I18n

    [Fact]
    public async Task Handler_DoesNotCreateItsOwnI18n_NorResetTheGlobalDefaultCulture()
    {
        using var fixture = new Fixture();
        var i18n = fixture.StartUiScope();
        i18n.SetCulture(EnUs, ZhCn);
        Assert.Equal(1, fixture.I18nInstancesCreated);

        // IHttpClientFactory 在第一次 CreateClient 时才构建 handler 管线
        await fixture.SendAsync();

        // 旧实现会在 factory 的 scope 里再造一个 I18n（默认 en-US），并在构造时把进程级默认文化重置成 en-US
        Assert.Equal(1, fixture.I18nInstancesCreated);
        Assert.Equal("zh-CN", CultureInfo.DefaultThreadCurrentUICulture?.Name);
        Assert.Equal("zh-CN", i18n.Culture.Name);
    }

    // ------------------------------------------------------------ 回退

    [Fact]
    public async Task WithoutAccessor_FallsBackToCurrentUICulture()
    {
        // 没有 IClientScopeServiceProviderAccessor（例如 Blazor Server 宿主）
        using var fixture = new Fixture(registerAccessor: false);
        CultureInfo.CurrentUICulture = ZhCn;

        Assert.Equal(Chinese, await fixture.SendAsync());
    }

    [Fact]
    public async Task AccessorNotInitializedYet_FallsBackToCurrentUICulture()
    {
        // accessor 已注册，但 InitializeApplicationAsync 还没把界面 scope 交给它：ServiceProvider 为 null
        using var fixture = new Fixture();
        Assert.Null(fixture.Accessor.ServiceProvider);
        CultureInfo.CurrentUICulture = ZhCn;

        Assert.Equal(Chinese, await fixture.SendAsync());
    }

    [Fact]
    public async Task UiScopeWithoutI18n_FallsBackToCurrentUICulture()
    {
        using var fixture = new Fixture();
        using var providerWithoutI18n = new ServiceCollection().BuildServiceProvider();
        fixture.Accessor.ServiceProvider = providerWithoutI18n;
        CultureInfo.CurrentUICulture = ZhCn;

        Assert.Equal(Chinese, await fixture.SendAsync());
    }

    [Fact]
    public async Task DisposedUiScope_FallsBackInsteadOfFailingTheRequest()
    {
        using var fixture = new Fixture();
        fixture.StartUiScope().SetCulture(EnUs, EnUs);
        fixture.DisposeUiScope();
        CultureInfo.CurrentUICulture = ZhCn;

        // 界面 scope 已释放时 GetService 会抛 ObjectDisposedException：不能因此让请求失败，回退到线程文化
        Assert.Equal(Chinese, await fixture.SendAsync());
    }

    // ------------------------------------------------------------ 不乱写头

    [Fact]
    public async Task ExplicitAcceptLanguage_IsNotOverwritten()
    {
        using var fixture = new Fixture();
        fixture.StartUiScope().SetCulture(EnUs, ZhCn);

        var header = await fixture.SendAsync(configure: request => request.Headers.AcceptLanguage.ParseAdd("fr-FR"));

        Assert.Equal("fr-FR", header);
    }

    [Fact]
    public async Task NoUsableCulture_DoesNotWriteTheHeader()
    {
        // InvariantCulture 的 Name 是空串：不写头，交给浏览器自己决定
        using var fixture = new Fixture(registerAccessor: false);
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Assert.Null(await fixture.SendAsync());
    }

    // ------------------------------------------------------------ 注册

    [Fact]
    public void Registration_IsIdempotent()
    {
        var services = new ServiceCollection();

        services.AddAcceptLanguageHandler();
        services.AddAcceptLanguageHandler();

        Assert.Single(services, d => d.ServiceType == typeof(AcceptLanguageHandler));
        Assert.Single(services, d => d.ServiceType == typeof(IHttpMessageHandlerBuilderFilter)
                                     && d.ImplementationType == typeof(AcceptLanguageHandlerFilter));
    }

    // ------------------------------------------------------------ 测试环境

    /// <summary>主 handler：记录请求真正发出时 Accept-Language 头的样子（与 DevTools 里看到的一致）。</summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? LastAcceptLanguage { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastAcceptLanguage = request.Headers.Contains("Accept-Language")
                ? request.Headers.AcceptLanguage.ToString()
                : null;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class Fixture : IDisposable
    {
        public const string DefaultClient = "default";
        public const string ClientA = "client-a";
        public const string ClientB = "client-b";

        private readonly RecordingHandler _recorder = new();
        private readonly ServiceProvider _root;
        private IServiceScope? _uiScope;
        private int _i18nInstancesCreated;

        public Fixture(bool registerAccessor = true)
        {
            var services = new ServiceCollection();

            // 与 AddMasaBlazor 一致：I18n 是 Scoped。数一下它被创建了几次——界面 scope 里的那一个之外不应该再有别的。
            services.AddScoped(_ => new MasaBlazorOptions());
            services.AddScoped(sp =>
            {
                Interlocked.Increment(ref _i18nInstancesCreated);
                return new I18n(sp.GetRequiredService<MasaBlazorOptions>());
            });

            if (registerAccessor)
            {
                // 与 Masa.Contrib.Authentication.Identity.BlazorWebAssembly 的注册一致（Singleton）
                services.AddSingleton<IClientScopeServiceProviderAccessor, ComponentsClientScopeServiceProviderAccessor>();
            }

            // 被测的注册：生产里 AddMasaStackComponent 调用的就是这个方法
            services.AddAcceptLanguageHandler();

            foreach (var name in new[] { DefaultClient, ClientA, ClientB })
            {
                services.AddHttpClient(name).ConfigurePrimaryHttpMessageHandler(() => _recorder);
            }

            _root = services.BuildServiceProvider();
        }

        public int I18nInstancesCreated => _i18nInstancesCreated;

        public ComponentsClientScopeServiceProviderAccessor Accessor =>
            (ComponentsClientScopeServiceProviderAccessor)_root.GetRequiredService<IClientScopeServiceProviderAccessor>();

        /// <summary>
        /// 模拟 WebAssemblyHost：建出界面 scope，并像 InitializeApplicationAsync 那样把它交给 accessor。
        /// 返回的是界面正在使用的 I18n。
        /// </summary>
        public I18n StartUiScope()
        {
            _uiScope = _root.CreateScope();
            Accessor.ServiceProvider = _uiScope.ServiceProvider;
            return _uiScope.ServiceProvider.GetRequiredService<I18n>();
        }

        public void DisposeUiScope() => _uiScope?.Dispose();

        public HttpClient CreateClient(string name = DefaultClient) =>
            _root.GetRequiredService<IHttpClientFactory>().CreateClient(name);

        public async Task<string?> SendAsync(string clientName = DefaultClient, Action<HttpRequestMessage>? configure = null)
        {
            using var client = CreateClient(clientName);
            return await SendAsync(client, configure);
        }

        /// <returns>请求到达主 handler 时的 Accept-Language；没有该头则为 null。</returns>
        public async Task<string?> SendAsync(HttpClient client, Action<HttpRequestMessage>? configure = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/ldap/connect-test");
            configure?.Invoke(request);

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return _recorder.LastAcceptLanguage;
        }

        public void Dispose()
        {
            _uiScope?.Dispose();
            _root.Dispose();
        }
    }
}
