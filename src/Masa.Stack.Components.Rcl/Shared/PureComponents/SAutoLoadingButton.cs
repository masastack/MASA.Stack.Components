namespace Masa.Stack.Components.Rcl.Shared.PureComponents;

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

    protected override void SetComponentClass()
    {
        base.SetComponentClass();

        CssProvider.Merge(delegate (CssBuilder cssBuilder)
        {
            cssBuilder.AddIf(BorderRadiusClass, () =>
            {
                return !(Class?.Split(' ').Contains(BorderRadiusClass) ?? false);
            });
        });
    }
}
