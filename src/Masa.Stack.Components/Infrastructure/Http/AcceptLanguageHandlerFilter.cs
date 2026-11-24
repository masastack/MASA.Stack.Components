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
            // 注意：builder.Services 是 IHttpClientFactory 为构建管线新建的 DI scope，不是界面 scope；
            // handler 注册为 Transient（每条管线一个新实例），界面语言由它自己通过 IClientScopeServiceProviderAccessor 读取
            builder.AdditionalHandlers.Add(builder.Services.GetRequiredService<AcceptLanguageHandler>());
        };
    }
}

