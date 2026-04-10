namespace Masa.Stack.Components.OpenTelemetry.Exporter;

internal class WasmLogExporter(HttpClient httpClient, string url) : BaseExporter<LogRecord>
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _url = url;
    private readonly string _scopeVersion = typeof(WasmLogExporter).Assembly.GetName().Version?.ToString()!;

    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        using var suppressScope = SuppressInstrumentationScope.Begin();
        var resource = this.ParentProvider?.GetResource();
        var resourceAttributes = resource?.Attributes;

        foreach (var record in batch)
        {
            var scopeName = record.CategoryName ?? MasaBlazorWasmConstants.MasaBlazorWasmActivitySourceName;
            object scope = string.IsNullOrEmpty(_scopeVersion)
                ? new { name = scopeName }
                : new { name = scopeName, version = _scopeVersion };

            var payload = new
            {
                resourceLogs = new[]
            {
                new {
                    resource = new {
                        attributes = resourceAttributes?.Select(a => new { key = a.Key, value = new { stringValue = a.Value.ToString() } })
                    },
                    scopeLogs = new[] {
                        new {
                            scope,
                            logRecords = new[] {
                                new {
                                   timeUnixNano = (new DateTimeOffset(record.Timestamp).ToUnixTimeMilliseconds() * 1000000).ToString(),
                                    severityNumber = MapSeverityNumber(record.LogLevel),
                                    severityText = record.LogLevel.ToString(),
                                    body = new { stringValue = record.FormattedMessage ?? record.Body },
                                    attributes = record.Attributes?
                                        .Select(a => new { key = a.Key, value = new { stringValue = a.Value?.ToString() } })
                                        .ToList(),
                                    traceId = record.TraceId.ToHexString(),
                                    spanId = record.SpanId.ToHexString()
                                }
                            }
                        }
                    }
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
        }
        return ExportResult.Success;
    }

    private static int MapSeverityNumber(LogLevel level) => level switch
    {
        LogLevel.Trace => 1,
        LogLevel.Debug => 5,
        LogLevel.Information => 9,
        LogLevel.Warning => 13,
        LogLevel.Error => 17,
        LogLevel.Critical => 21,
        _ => 0
    };
}
