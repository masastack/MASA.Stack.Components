namespace Masa.Stack.Components;

public class JsInitVariables : IAsyncDisposable
{
    IJSObjectReference? _helper;
    IJSRuntime _jsRuntime;
    TimeSpan _timezoneOffset;
    ProtectedLocalStorage _storage;
    static readonly string _timezoneOffsetKey = "timezoneOffset";

    public TimeSpan TimezoneOffset
    {
        get => _timezoneOffset;
        set
        {
            _storage.SetAsync(_timezoneOffsetKey, value.TotalMinutes);
            _timezoneOffset = value;
        }
    }

    public JsInitVariables(IJSRuntime jsRuntime, ProtectedLocalStorage storage)
    {
        _jsRuntime = jsRuntime;
        _storage = storage;
    }

    public async Task SetTimezoneOffset()
    {
        var timezoneOffsetResult = await _storage.GetAsync<double>(_timezoneOffsetKey);
        if(timezoneOffsetResult.Success)
        {
            TimezoneOffset = TimeSpan.FromMinutes(timezoneOffsetResult.Value);
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
