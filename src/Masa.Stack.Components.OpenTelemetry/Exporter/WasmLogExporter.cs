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
                                    attributes = BuildAttributes(record),
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

    /// <summary>
    /// 与 OtlpLogRecordTransformer 一致：除 record.Attributes 外，通过 ForEachScope 写入 scope 中的键值（IncludeScopes 时）。
    /// </summary>
    private static List<object>? BuildAttributes(LogRecord record)
    {
        var acc = new LogAttributeAccumulator();

        if (record.Attributes != null)
        {
            foreach (var a in record.Attributes)
                acc.Add(a.Key, a.Value?.ToString());
        }

        record.ForEachScope(
            static (scope, state) =>
            {
                foreach (var item in scope)
                {
                    if (string.IsNullOrEmpty(item.Key) || item.Key.Equals("{OriginalFormat}", StringComparison.Ordinal))
                        continue;
                    state.Add(item.Key, item.Value?.ToString());
                }
            },
            acc);

        return acc.List;
    }

    private sealed class LogAttributeAccumulator
    {
        public List<object>? List { get; private set; }

        public void Add(string key, string? value)
        {
            if (string.IsNullOrEmpty(key))
                return;
            List ??= new List<object>();
            List.Add(new { key, value = new { stringValue = value } });
        }
    }
}
