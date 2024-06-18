namespace Masa.Stack.Components;

public class SAutoLoadingButton : MButton
{
    [Parameter]
    public string BorderRadiusClass { get; set; } = "rounded-pill";

    [Parameter]
    public bool DisableLoading { get; set; }

    CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Color = "primary";
        await base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        var originalOnClick = OnClick;

        if (OnClick.HasDelegate)
        {
            OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async (args) =>
            {
                Loading = DisableLoading is false;
                Disabled = true;

                try
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource = new CancellationTokenSource();
                    await Task.Delay(500, _cancellationTokenSource.Token);

                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    await originalOnClick.InvokeAsync(args);
                }
                finally
                {
                    Loading = false;
                    Disabled = false;
                    StateHasChanged();
                }
            });
        }
    }

    protected override IEnumerable<string> BuildComponentClass()
    {
        if (!(Class?.Split(' ').Contains(BorderRadiusClass) ?? false))
        {
            return base.BuildComponentClass().Concat(new[] { BorderRadiusClass });
        }

        return base.BuildComponentClass();
    }
}
