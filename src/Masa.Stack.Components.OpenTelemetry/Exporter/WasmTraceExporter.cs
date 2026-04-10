namespace Masa.Stack.Components.OpenTelemetry.Exporter;

internal class WasmTraceExporter(HttpClient httpClient, string url) : BaseExporter<Activity>
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _url = url;
    private readonly string _scopeVersion = typeof(WasmLogExporter).Assembly.GetName().Version?.ToString()!;

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

    static object SpanModel(Activity activity) => new
    {
        traceId = activity.TraceId.ToHexString(),
        spanId = activity.SpanId.ToHexString(),
        parentSpanId = activity.ParentSpanId.ToHexString(),
        name = activity.DisplayName,
        kind = (int)activity.Kind,
        startTimeUnixNano = (activity.StartTimeUtc.Ticks - 621355968000000000).ToString() + "00", // 轉換為納秒字串
        endTimeUnixNano = ((activity.StartTimeUtc.Ticks + activity.Duration.Ticks - 621355968000000000)).ToString() + "00",
        attributes = activity.TagObjects
                    .Select(t => new { key = t.Key, value = new { stringValue = t.Value?.ToString() } })
                    .ToList(),
        status = new { code = (int)activity.Status },
    };
}