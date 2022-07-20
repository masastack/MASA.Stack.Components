namespace Masa.Stack.Components;

public class AutoLoadingButton : MButton
{
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
                Loading = true;
                Disabled = true;

                try
                {
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
}
