namespace Masa.Stack.Components;

public class STooltip : MTooltip
{
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Color = "emphasis2";
        Right = true;
        ContentClass = "rounded-2";
        ContentStyle = "border: 1px solid var(--ms-border);";

        await base.SetParametersAsync(parameters);
    }
}
