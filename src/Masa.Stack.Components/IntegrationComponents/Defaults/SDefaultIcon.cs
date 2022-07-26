namespace Masa.Stack.Components;

public class SDefaultIcon : MIcon
{
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Size = 20;

        await base.SetParametersAsync(parameters);
    }
}
