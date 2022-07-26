namespace Masa.Stack.Components;

public class SIcon : MIcon
{
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Size = 20;

        await base.SetParametersAsync(parameters);
    }
}
