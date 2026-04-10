namespace Masa.Stack.Components.OpenTelemetry;

/// <summary>
/// 出站前移除 W3C / baggage 等传播头，避免采集请求触发 CORS 预检或被服务端拒绝。
/// （运行时 Diagnostics 与 OTel Http 注入可能发生在调用链中任意一层，在发送前统一剥离最稳妥。）
/// </summary>
internal sealed class StripPropagatedTraceHeadersHandler : DelegatingHandler
{
    public StripPropagatedTraceHeadersHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove("traceparent");
        request.Headers.Remove("tracestate");
        request.Headers.Remove("baggage");
        return base.SendAsync(request, cancellationToken);
    }
}
