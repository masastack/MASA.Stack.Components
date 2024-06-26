namespace Masa.Stack.Components.Standalone;

public class SBtn : MButton
{
    [Parameter] public bool AutoLoading { get; set; }

    private EventCallback<MouseEventArgs>? _cachedOnClick;
    private CancellationTokenSource? _cancellationTokenSource;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Assume AutoLoading will not change
        if (AutoLoading)
        {
            if (_cachedOnClick != null)
            {
                OnClick = _cachedOnClick.Value;
            }
            else if (OnClick.HasDelegate)
            {
                var originalOnClick = OnClick;

                _cachedOnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async (args) =>
                {
                    Loading = true;
                    Disabled = true;
                    StateHasChanged();

                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource = new CancellationTokenSource();
                    try
                    {
                        await Task.Delay(100, _cancellationTokenSource.Token);
                        await originalOnClick.InvokeAsync(args);
                    }
                    catch (TaskCanceledException)
                    {
                        // ignored
                    }
                    finally
                    {
                        Loading = false;
                        Disabled = false;
                        StateHasChanged();
                    }
                });

                OnClick = _cachedOnClick.Value;
            }
        }
    }
}