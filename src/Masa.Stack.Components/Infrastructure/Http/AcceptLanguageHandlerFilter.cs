using Microsoft.Extensions.Http;

namespace Masa.Stack.Components.Infrastructure.Http;

/// <summary>
/// HttpMessageHandler 构建过滤器，用于全局添加 AcceptLanguageHandler
/// 在 .NET 6/7 中实现类似 ConfigureHttpClientDefaults 的效果
/// </summary>
internal class AcceptLanguageHandlerFilter : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            // 先执行下一个过滤器
            next(builder);

            // 添加 AcceptLanguageHandler 到所有 HttpClient
            builder.AdditionalHandlers.Add(builder.Services.GetRequiredService<AcceptLanguageHandler>());
        };
    }
}

