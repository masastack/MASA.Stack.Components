namespace Masa.Stack.Components;

public class SAutoLoadingButton : MButton
{
    public string BorderRadiusClass { get; set; } = "rounded-pill";

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
