namespace Masa.Stack.Components;

public class DefaultSelect<TItem, TItemValue, TValue> : MSelect<TItem, TItemValue, TValue>
{
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Dense = true;
        Height = 48;
        HideDetails = "auto";
        Outlined = true;

        await base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Dense && Height == 48)
        {
            Class ??= string.Empty;
            if (!Class.Contains("m-input--dense-48"))
            {
                Class += " m-input--dense-48";
            }
        }
    }
}
