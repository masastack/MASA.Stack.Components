namespace Masa.Stack.Components.OpenTelemetry.Options;

public sealed class MasaBlazorWasmConstants
{
    private MasaBlazorWasmConstants() { }

    public const string MasaBlazorWasmActivitySourceName = "MasaBlazorWasm";

    internal readonly static JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public const string LastLoginUserId = "userid";
    public const string BlazorClientType = "maui-blazor";
    public const string BlazorPageTitle = "client.title";
    public const string BlazorPagePath = "client.path";
    public const string BlazorPageRouter = "client.path.route";
    public const string BlazorPageFirstShowTime = "client.show.startTime";
    public const string BlazorPageFromPath = "from.path";
    public const string BlazorPageFromTitle = "from.title";
    public const string BlazorPageToPath = "to.path";
    public const string BlazorPageModuleName = "client.module.name";
    public const string BlazorPageModuleCode = "client.module.code";
    public const string BlazorPageModuleVersion = "client.module.version";
    public const string BlazorPageModuleFromVersion = "from.module.version";
    public const string BlazorPageModuleFromCode = "from.module.code";
    public const string BlazorPageModuleToVersion = "to.module.version";
    public const string BlazorPageModuleToCode = "to.module.code";
    public const string BlazorPageTraceId = "masa.ui.traceid";
    public const string CallerHeaderTraceId = "masa-ui-traceid";

    public const string SessionUserId = "enduser.id";
    public const string SessionUserName = "enduser.nick_name";

    public const string ClientIp = "http.client_ip";
    public const string BlazorPageSessionId = "client.session_id";

    public const string ExceptionType = "exception.type";
    public const string ExceptionMessage = "exception.message";

    public const string HttpRequestUrlFull = "http.url";
    public const string HttpRequestMethod = "http.method";
    public const string HttpRequestTarget = "http.target";
    public const string HttpRequestStatusCode = "http.status_code";
    public const string HttpRequestSchema = "http.scheme";
    public const string HttpRequestUserAgent = "http.user_agent";
    public const string HttpRequestServer = "server.address";
    public const string HttpRequestServerPort = "server.port";
    public const string HttpRequestToken = "Authentication";
    public const string HttpResponseStatusCode = "http.response.status_code";
    public const string HttpRequestContentBody = "http.request_content_body";
    public const string HttpRequestFlavor = "http.flavor";
    public const string HttpRequestReferer = "http.referer";
    public const string HttpRequestUserFriendlyResult = "http.userfriendly";

    public static long UnixTimespan(DateTime time)
    {
        DateTimeOffset offset = new(time.ToLocalTime());
        return offset.ToUnixTimeMilliseconds();
    }
}