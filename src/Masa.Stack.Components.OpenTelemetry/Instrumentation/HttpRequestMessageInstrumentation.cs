namespace Masa.Stack.Components.OpenTelemetry.Instrumentation;

internal static class HttpRequestMessageInstrumentation
{
    internal static ILogger? Logger { get; set; }

    internal const long MaxBodySize = 4L << 20;//4MB
    private static readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler = new();

    public static void OnHttpMessageRequest(Activity activity, HttpRequestMessage requestMessage)
    {
        if (activity == null) return;
        activity.SetTag(OpenTelemetryAttributeName.Http.SCHEME, requestMessage.RequestUri?.Scheme);

        var blazorAcitivity = MasaBlazorActivityContent.CurrentActivity;
        var sourceUrl = blazorAcitivity?.GetTagItem(MasaBlazorWasmConstants.BlazorPagePath)?.ToString();
        if (!string.IsNullOrEmpty(sourceUrl))
        {
            requestMessage.Headers.TryAddWithoutValidation("Referer", sourceUrl);
            activity.SetTag(MasaBlazorWasmConstants.HttpRequestReferer, sourceUrl);
        }
        activity.SetTag(OpenTelemetryAttributeName.Host.NAME, Dns.GetHostName());
        activity.SetTag(OpenTelemetryAttributeName.Authorization.VALUE, requestMessage.Headers.Authorization);
        activity.SetTag(MasaBlazorWasmConstants.HttpRequestSchema, requestMessage.RequestUri?.Scheme);
        activity.SetTag(MasaBlazorWasmConstants.HttpRequestMethod, requestMessage.Method);
        activity.SetTag(MasaBlazorWasmConstants.HttpRequestFlavor, requestMessage.Version);
        activity.SetTag(MasaBlazorWasmConstants.HttpRequestServer, requestMessage.RequestUri?.Host);
        activity.SetTag(MasaBlazorWasmConstants.HttpRequestServerPort, requestMessage.RequestUri?.Port);
        activity.SetTag(MasaBlazorWasmConstants.HttpRequestUrlFull, requestMessage.RequestUri?.OriginalString);
        var uiTraceId = blazorAcitivity?.TraceId.ToString();
        if (!string.IsNullOrEmpty(uiTraceId))
        {
            requestMessage.Headers.TryAddWithoutValidation(MasaBlazorWasmConstants.CallerHeaderTraceId, uiTraceId);
            activity.SetTag(MasaBlazorWasmConstants.BlazorPageTraceId, uiTraceId);
        }

        if (requestMessage.Content != null)
        {
            SetActivityBody(activity, requestMessage.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult(),
                GetHttpRequestMessageEncoding(requestMessage)).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        if (!string.IsNullOrEmpty(requestMessage.Headers?.Authorization?.Parameter))
        {
            activity.SetTag(OpenTelemetryAttributeName.Authorization.VALUE, requestMessage.Headers?.Authorization?.Parameter);
            if (requestMessage.Headers!.Authorization.Scheme == "Bearer")
            {
                var token = _jwtSecurityTokenHandler.ReadJwtToken(requestMessage.Headers?.Authorization?.Parameter);
                var userId = token?.Claims.FirstOrDefault(claim => claim.Type == IdentityClaimConsts.USER_ID)?.Value;
                if (!string.IsNullOrEmpty(userId))
                    activity.SetTag(OpenTelemetryAttributeName.EndUser.ID, userId);
                var userName = token?.Claims.FirstOrDefault(claim => claim.Type == IdentityClaimConsts.USER_NAME)?.Value;
                if (!string.IsNullOrEmpty(userName))
                    activity.SetTag(OpenTelemetryAttributeName.EndUser.USER_NICK_NAME, userName);
            }
        }
        if (requestMessage.RequestUri != null)
        {
            activity.SetTag(OpenTelemetryAttributeName.Http.ServerAddress, requestMessage.RequestUri.Host);
            activity.SetTag(OpenTelemetryAttributeName.Http.ServerPort, requestMessage.RequestUri.Port);
            activity.SetTag(OpenTelemetryAttributeName.Http.SCHEME, requestMessage.RequestUri.Scheme);
            activity.SetTag(OpenTelemetryAttributeName.Http.REQUEST_URL, requestMessage.RequestUri?.OriginalString);
        }
    }

    public static void OnHttpResponseMessage(Activity activity, HttpResponseMessage httpResponseMessage)
    {
        if (activity == null) return;
        activity.SetTag(OpenTelemetryAttributeName.Host.NAME, Dns.GetHostName());
        activity.SetTag(MasaBlazorWasmConstants.HttpResponseStatusCode, (int)httpResponseMessage.StatusCode);
        if (httpResponseMessage.StatusCode - 299 == 0 || httpResponseMessage.StatusCode - 599 == 0)
        {
            var text = httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            if (httpResponseMessage.StatusCode - 599 == 0)
            {
                var result = JsonSerializer.Deserialize<LonsidUserFriendlyDto>(text, MasaBlazorWasmConstants.JsonSerializerOptions)!;
                activity.SetTag(MasaBlazorWasmConstants.HttpRequestUserFriendlyResult, result.Error.Message);
            }
        }
    }

    private static Encoding? GetHttpRequestMessageEncoding(HttpRequestMessage httpRequest)
    {
        if (httpRequest.Content != null)
        {
            var encodeStr = httpRequest.Content.Headers?.ContentType?.CharSet;

            if (!string.IsNullOrEmpty(encodeStr))
            {
                return Encoding.GetEncoding(encodeStr);
            }
        }

        return null;
    }

    private static async Task SetActivityBody(Activity activity, Stream inputSteam, Encoding? encoding = null)
    {
        (long length, string? body) = await inputSteam.ReadAsStringAsync(encoding);

        if (length <= 0)
            return;
        if (length - MaxBodySize > 0)
        {
            Logger?.LogInformation("Request body in base64 encode: {Body}", body);
        }
        else
        {
            activity.SetTag(OpenTelemetryAttributeName.Http.REQUEST_CONTENT_BODY, body);
        }
    }

    public static bool FilterHttpRequest(HttpRequestMessage requestMessage, string otelUrl)
    {
        var url = requestMessage.RequestUri?.AbsoluteUri;
        if (string.IsNullOrEmpty(url)) return false;
        string[] ignores = [".js", ".css", ".json", ".woff", ".woff2", ".ttf", ".otf", ".html", ".htm", ".mp4"];
        if (ignores.Any(item => url.EndsWith(item, StringComparison.CurrentCultureIgnoreCase)))
            return false;
        // 过滤掉导出日志时发送的请求，避免死循环
        if (requestMessage.RequestUri != null && requestMessage.RequestUri.ToString().Contains(GetOtelUrl(otelUrl, isTrace: true)))
            return false;
        if (requestMessage.RequestUri != null && requestMessage.RequestUri.ToString().Contains(GetOtelUrl(otelUrl, isLog: true)))
            return false;
        return true;
    }

    public static string GetOtelUrl(string host, bool isTrace = false, bool isLog = false)
    {
        if (string.IsNullOrEmpty(host))
            host = "/";
        host = host.EndsWith('/') ? host : host + "/";
        if (isTrace)
            return $"{host}v1/traces";
        if (isLog)
            return $"{host}v1/logs";
        return host;
    }
}