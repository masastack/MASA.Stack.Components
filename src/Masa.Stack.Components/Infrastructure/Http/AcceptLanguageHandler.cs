namespace Masa.Stack.Components.Infrastructure.Http;

/// <summary>
/// HTTP 消息处理器，用于自动为请求添加 Accept-Language 头
/// </summary>
/// <remarks>
/// 语言来源必须是「界面正在使用的那个 <see cref="I18n"/>」——<c>GlobalConfig.Culture</c> 切换/恢复语言时改的就是它。
/// <para>
/// 这里<b>不能</b>通过构造函数直接注入 <see cref="I18n"/>：<see cref="IHttpClientFactory"/> 会在自己新建的 DI scope
/// 里构建 handler 管线，Scoped 的 <see cref="I18n"/> 在那个 scope 里会得到一个与界面无关的全新实例——
/// 它的默认语言是 en-US（于是不管界面是什么语言，发出去的永远是 <c>en-US, en; q=0.9</c>），
/// 而且构造时还会把全局的 <see cref="CultureInfo.DefaultThreadCurrentUICulture"/> 重置成 en-US。
/// </para>
/// <para>
/// 所以改为通过 <see cref="IClientScopeServiceProviderAccessor"/> 拿到界面 scope 的 <see cref="I18n"/>
/// （与 <see cref="I18nCache"/>、<see cref="DynamicTranslateProvider"/> 的做法一致）。
/// 没有这个 accessor（如 Blazor Server 宿主）、或界面 scope 尚未初始化/已释放时，回退到
/// <see cref="CultureInfo.CurrentUICulture"/>。
/// </para>
/// </remarks>
public class AcceptLanguageHandler : DelegatingHandler
{
    private readonly IClientScopeServiceProviderAccessor? _clientScopeAccessor;

    /// <param name="clientScopeAccessor">
    /// 界面 scope 的访问器；未注册时为 null（DI 会使用默认值）。
    /// </param>
    public AcceptLanguageHandler(IClientScopeServiceProviderAccessor? clientScopeAccessor = null)
    {
        _clientScopeAccessor = clientScopeAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 设置 Accept-Language 头（后端根据这个判断返回什么语言）
        // 包含降级语言，确保后端总能找到合适的语言
        // 已经显式带了该头的请求不覆盖
        if (!request.Headers.Contains("Accept-Language"))
        {
            var culture = ResolveCultureName();

            // 拿不到有效语言（如 InvariantCulture 的 Name 为空）就不写头，交给浏览器自己决定
            if (!string.IsNullOrWhiteSpace(culture))
            {
                var acceptLanguage = BuildAcceptLanguageHeader(culture);
                request.Headers.Add("Accept-Language", acceptLanguage);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// 每次发请求时实时读取当前语言（不缓存），这样界面切换语言后下一个请求立即生效。
    /// </summary>
    private string ResolveCultureName()
    {
        try
        {
            // 界面 scope 的 I18n：与 GlobalConfig.Culture 的 setter 修改的是同一个实例
            var uiCulture = _clientScopeAccessor?.ServiceProvider?.GetService<I18n>()?.Culture?.Name;
            if (!string.IsNullOrWhiteSpace(uiCulture))
            {
                return uiCulture;
            }
        }
        catch (ObjectDisposedException)
        {
            // 界面 scope 已释放（例如微前端正在卸载），回退到线程文化
        }

        return CultureInfo.CurrentUICulture.Name;
    }

    /// <summary>
    /// 构建 Accept-Language 头，包含降级语言
    /// </summary>
    /// <param name="culture">当前文化，如 zh-CN, en-US</param>
    /// <returns>Accept-Language 头值，如 "zh-CN,zh;q=0.9,en;q=0.8"</returns>
    private static string BuildAcceptLanguageHeader(string culture)
    {
        var languages = new List<string>
        {
            // 1. 添加完整的文化代码（优先级 1.0，最高）
            culture
        };

        // 2. 如果是特定区域的语言（如 zh-CN），添加通用语言（如 zh）
        if (culture.Contains("-"))
        {
            var languageOnly = culture.Split('-')[0];
            languages.Add($"{languageOnly};q=0.9");
        }

        // 3. 添加英语作为通用降级语言（如果当前不是英语）
        if (!culture.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            languages.Add("en;q=0.8");
        }

        return string.Join(",", languages);
    }
}
