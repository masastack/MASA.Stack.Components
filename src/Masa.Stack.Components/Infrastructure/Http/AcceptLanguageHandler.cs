namespace Masa.Stack.Components.Infrastructure.Http;

/// <summary>
/// HTTP 消息处理器，用于自动为请求添加 Accept-Language 头
/// </summary>
public class AcceptLanguageHandler : DelegatingHandler
{
    private readonly I18n _i18n;

    public AcceptLanguageHandler(I18n i18n)
    {
        _i18n = i18n;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 从当前 I18n 获取语言设置
        var culture = _i18n.Culture.Name;

        // 设置 Accept-Language 头（后端根据这个判断返回什么语言）
        // 包含降级语言，确保后端总能找到合适的语言
        if (!request.Headers.Contains("Accept-Language"))
        {
            var acceptLanguage = BuildAcceptLanguageHeader(culture);
            request.Headers.Add("Accept-Language", acceptLanguage);
        }

        return await base.SendAsync(request, cancellationToken);
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

