namespace Masa.Stack.Components;

public class STooltip : MTooltip
{
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Color = "inverse-surface";
        Right = true;
        ContentClass = "rounded-2";
        ContentStyle = "border: 1px solid rgba(var(--m-theme-outline-variant));";

        await base.SetParametersAsync(parameters);
    }
}
