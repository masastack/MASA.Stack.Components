namespace Masa.Stack.Components.OpenTelemetry;

using global::OpenTelemetry.Context.Propagation;

/// <summary>
/// 保留從載體解析 TraceContext（Extract），但不在出站請求上注入 traceparent/tracestate（Inject 為空），
/// 避免 Blazor WASM 跨域 API 因非簡單頭觸發 CORS 預檢失敗。
/// </summary>
internal sealed class NonInjectingTraceContextPropagator : TextMapPropagator
{
    private static readonly TextMapPropagator TraceContext = new TraceContextPropagator();

    public override ISet<string> Fields => TraceContext.Fields;

    public override PropagationContext Extract<T>(PropagationContext context, T carrier, Func<T, string, IEnumerable<string>>? getter)
        => TraceContext.Extract(context, carrier, getter!);

    public override void Inject<T>(PropagationContext context, T carrier, Action<T, string, string>? setter)
    {
        // 不調用 setter：不向 HttpClient 等出站請求寫入追蹤頭
    }
}
