namespace Masa.Stack.Components.OpenTelemetry.Exporter;

internal class WasmTraceExporter(HttpClient httpClient, string url) : BaseExporter<Activity>
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _url = url;
    private readonly string _scopeVersion = typeof(WasmTraceExporter).Assembly.GetName().Version?.ToString()!;

    public override ExportResult Export(in Batch<Activity> batch)
    {
        var resourceAttributes = this.ParentProvider?.GetResource()?.Attributes
            .Select(a => new { key = a.Key, value = new { stringValue = a.Value?.ToString() } })
            .ToList();

        var activities = new List<Activity>();
        foreach (var a in batch)
            activities.Add(a);

        var scopeSpansList = new List<object>();

        foreach (var group in activities.GroupBy(a => new { a.Source.Name, Version = string.IsNullOrEmpty(a.Source.Version) ? _scopeVersion : a.Source.Version }))
        {
            var spans = group.Select(SpanModel).ToList();
            object scope = string.IsNullOrEmpty(group.Key.Version)
                ? new { name = group.Key.Name }
                : new { name = group.Key.Name, version = group.Key.Version! };
            scopeSpansList.Add(new { scope, spans });
        }

        var payload = new
        {
            resourceSpans = new[]
            {
                new
                {
                    resource = new { attributes = resourceAttributes },
                    scopeSpans = scopeSpansList
                }
            }
        };

        _ = Task.Run(async () =>
        {
            using var suppressScope = SuppressInstrumentationScope.Begin();
            var previous = Activity.Current;
            try
            {
                Activity.Current = null;
                await _httpClient.PostAsJsonAsync(_url, payload).ConfigureAwait(false);
            }
            finally
            {
                Activity.Current = previous;
            }
        });
        return ExportResult.Success;
    }

    /// <summary>
    /// OTLP SpanKind 与 <see cref="ActivityKind"/> 数值不一致，不能直接强转。
    /// Proto: UNSPECIFIED=0, INTERNAL=1, SERVER=2, CLIENT=3, PRODUCER=4, CONSUMER=5。
    /// .NET: Internal=0, Server=1, Client=2, Producer=3, Consumer=4。
    /// </summary>
    private static int ToOtlpSpanKind(ActivityKind kind) => kind switch
    {
        ActivityKind.Internal => 1,
        ActivityKind.Server => 2,
        ActivityKind.Client => 3,
        ActivityKind.Producer => 4,
        ActivityKind.Consumer => 5,
        _ => 0
    };

    static object SpanModel(Activity activity) => new
    {
        traceId = activity.TraceId.ToHexString(),
        spanId = activity.SpanId.ToHexString(),
        parentSpanId = activity.ParentSpanId.ToHexString(),
        name = activity.DisplayName,
        kind = ToOtlpSpanKind(activity.Kind),
        startTimeUnixNano = (activity.StartTimeUtc.Ticks - 621355968000000000).ToString() + "00", // 轉換為納秒字串
        endTimeUnixNano = (activity.StartTimeUtc.Ticks + activity.Duration.Ticks - 621355968000000000).ToString() + "00",
        attributes = activity.TagObjects
                    .Select(t => new { key = t.Key, value = new { stringValue = t.Value?.ToString() } })
                    .ToList(),
        status = new { code = (int)activity.Status },
    };
}