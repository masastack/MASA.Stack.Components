namespace Masa.Stack.Components;

public class JsInitVariables : IAsyncDisposable
{
    private const string TimezoneOffsetKey = "timezoneOffset";
    readonly IJSRuntime _jsRuntime;
    readonly CookieStorage _storage;
    TimeSpan _timezoneOffset;
    IJSObjectReference? _helper;
    public event Action? TimezoneOffsetChanged;

    public TimeSpan TimezoneOffset
    {
        get => _timezoneOffset;
        set
        {
            _storage.SetAsync(TimezoneOffsetKey, value.TotalMinutes);
            _timezoneOffset = value;
            TimezoneOffsetChanged?.Invoke();
        }
    }

    public JsInitVariables(IJSRuntime jsRuntime, CookieStorage storage, IHttpContextAccessor httpContextAccessor)
    {
        _jsRuntime = jsRuntime;
        _storage = storage;
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var timezoneOffsetResult = httpContext.Request.Cookies[TimezoneOffsetKey];
            _timezoneOffset = TimeSpan.FromMinutes(Convert.ToDouble(timezoneOffsetResult));
        }
    }

    public async Task SetTimezoneOffset()
    {
        var timezoneOffsetResult = await _storage.GetAsync(TimezoneOffsetKey);
        if (string.IsNullOrEmpty(timezoneOffsetResult) is false)
        {
            TimezoneOffset = TimeSpan.FromMinutes(Convert.ToDouble(timezoneOffsetResult));
            return;
        }
        _helper ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Masa.Stack.Components/js/jsInitVariables/jsInitVariables.js");
        var offset = await _helper.InvokeAsync<double>("getTimezoneOffset");
        TimezoneOffset = TimeSpan.FromMinutes(-offset);
    }

    public async ValueTask DisposeAsync()
    {
        if (_helper is not null)
            await _helper.DisposeAsync();
    }
}
