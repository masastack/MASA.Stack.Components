namespace Masa.Stack.Components;

public class SAutoComplete<TItem, TItemValue, TValue> : MAutocomplete<TItem, TItemValue, TValue>
{
    [Parameter]
    public bool Small { get; set; }

    [Parameter]
    public bool Medium { get; set; }

    [Parameter]
    public bool Large { get; set; }

    [Parameter]
    public bool Required { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Dense = true;
        HideDetails = "auto";
        Outlined = true;
        HideSelected = true;
        Color = "primary";
        Style = "";
        Class = "";
        await base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        Class ??= "";
        if (Large is false && Small is false) Medium = true;
        if (Dense is true)
        {
            if (Large)
            {
                MinHeight = 56;
                if (Class.Contains("m-input--dense-56") is false)
                {
                    Class += " m-input--dense-56";
                }
            }
            else if (Medium)
            {
                MinHeight = 48;
                if (Class.Contains("m-input--dense-48") is false)
                {
                    Class += " m-input--dense-48";
                }
            }
            else if (Small)
            {
                MinHeight = 40;
                if (Class.Contains("m-input--dense-40") is false)
                {
                    Class += " m-input--dense-40";
                }
            }
        }

        if (Required && PrependInnerContent == default)
        {
            PrependInnerContent = builder =>
            {
                builder.OpenElement(0, "label");
                builder.AddAttribute(1, "class", "red--text");
                builder.AddContent(2, "*");
                builder.CloseElement();
            };
        }
    }
}
