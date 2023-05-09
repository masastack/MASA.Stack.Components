namespace Masa.Stack.Components;

public partial class SIcon : MIcon
{
    [Parameter]
    public string? Tooltip { get; set; }

    [Inject]
    private I18n? I18N { get; set; }

    private static readonly Dictionary<string, string> IconI18N = new()
    {
        { "mdi-pencil" , "Edit" },
        { "mdi-check" , "Save" },
        { "mdi-pin" , "Pin" },
        { "mdi-magnify" , "Search" },
        { "mdi-dots-horizontal" , "More" },
        { "mdi-dots-vertical" , "More" },
        { "far fa-copy" , "Copy" },
        { "mdi-chevron-down" , "Expand" },
        { "mdi-plus" , "Add" },
        { "mdi-delete" , "Delete" },
        { "mdi-link-variant" , "Relevance" },
        { "mdi-close" , "Close" },
        { "mdi-keyboard-backspace", "PreviousStep" },
        { "mdi-chevron-left", "PreviousStep"},
        { "mdi-chevron-right", "NextStep"},
        { "mdi-star","Favorite" },
        { "mdi-star-outline","CancelFavorite" },
    };

    [Parameter]
    public bool IsDefaultToolTip { get; set; } = true;

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Size = 20;
        await base.SetParametersAsync(parameters);
    }

    private void InitDefaultToolTip()
    {
        if (IsDefaultToolTip && Tooltip is null && Icon is not null)
        {
            Icon = Icon.Trim();
            if (IconI18N.TryGetValue(Icon, out string? value))
            {
                Tooltip = I18N?.T(value);
            }
            else
            {
                value = IconI18N.FirstOrDefault(x => Icon.Contains(x.Key)).Value;
                if (value is not null)
                {
                    Tooltip = I18N?.T(value);
                }
            }
        }
    }
}
