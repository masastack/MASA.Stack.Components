namespace Masa.Stack.Components;

public partial class SIcon : MIcon
{
    [Parameter]
    public string? Tooltip { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Size = 20;
        await base.SetParametersAsync(parameters);
    }
}
