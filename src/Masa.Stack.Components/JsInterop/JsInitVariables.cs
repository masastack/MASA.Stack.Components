namespace Masa.Stack.Components;

public class JsInitVariables: IAsyncDisposable
{
    IJSObjectReference? _helper;
    IJSRuntime _jsRuntime;

    public JsInitVariables(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public TimeSpan TimezoneOffset { get; private set; }

    public async Task SetTimezoneOffset()
    {
        _helper ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Masa.Stack.Components/js/jsInitVariables/jsInitVariables.js");
        var offset = await _helper.InvokeAsync<double>("getTimezoneOffset");
        TimezoneOffset = TimeSpan.FromMinutes(-offset);
    }

    public async ValueTask DisposeAsync()
    {
        if(_helper is not null)
            await _helper.DisposeAsync();
    }
}
