namespace Masa.Stack.Components;

public class DefaultTextfield<TValue> : MTextField<TValue>
{
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);

        HideDetails = "auto";
        Outlined = true;
    }
}
