namespace Masa.Stack.Components.Standalone;

public class SStatefulChip<TState> : MChip
{
    [Parameter] public TState? SuccessState { get; set; }

    [Parameter] public TState? ErrorState { get; set; }

    [Parameter] public TState? WarningState { get; set; }

    [Parameter] public TState? InfoState { get; set; }

    [Parameter] public TState? NeutralState { get; set; }

    [Parameter] public TState? PremiumState { get; set; }

    [Parameter] public TState? State { get; set; }

    /// <summary>
    /// Rule to determine the color of the chip.
    /// Accepts built-in states: "info", "success", "error", "warning", "neutral", "premium"
    /// and custom built-in colors.
    /// </summary>
    [Parameter] public Func<TState, string>? Rule { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Color is null)
        {
            string? color = null;

            if (Rule is not null)
            {
                color = GetStateCss(Rule(State));
            }
            else
            {
                if (SuccessState != null && EqualityComparer<TState>.Default.Equals(SuccessState, State))
                {
                    color = GetColorCss("green");
                }
                else if (ErrorState != null && EqualityComparer<TState>.Default.Equals(ErrorState, State))
                {
                    color = GetColorCss("red");
                }
                else if (WarningState != null && EqualityComparer<TState>.Default.Equals(WarningState, State))
                {
                    color = GetColorCss("orange");
                }
                else if (InfoState != null && EqualityComparer<TState>.Default.Equals(InfoState, State))
                {
                    color = GetColorCss("blue");
                }
                else if (NeutralState != null && EqualityComparer<TState>.Default.Equals(NeutralState, State))
                {
                    color = GetColorCss("grey");
                }
                else if (PremiumState != null && EqualityComparer<TState>.Default.Equals(PremiumState, State))
                {
                    color = GetColorCss("purple");
                }
            }

            Color = color;
        }
    }

    private static string? GetStateCss(string? stateOrColor)
    {
        if (string.IsNullOrWhiteSpace(stateOrColor))
        {
            return null;
        }
        
        var color = stateOrColor switch
        {
            "info" => "blue",
            "success" => "green",
            "error" => "red",
            "warning" => "orange",
            "neutral" => "grey",
            "premium" => "purple",
            _ => stateOrColor
        };

        return GetColorCss(color);
    }

    private static string GetColorCss(string color)
    {
        return string.Format("{0} lighten-5 {0}--text", color);
    }
}