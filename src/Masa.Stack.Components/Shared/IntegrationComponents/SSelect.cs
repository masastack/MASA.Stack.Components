using Masa.Stack.Components.Extensions;

namespace Masa.Stack.Components;

public class SSelect<TItem, TItemValue, TValue> : MSelect<TItem, TItemValue, TValue>
{
    [Parameter]
    public bool Small { get; set; }

    [Parameter]
    public bool Medium { get; set; }

    [Parameter]
    public bool Large { get; set; }

    [Parameter]
    public string? Tooltip { get; set; }

    [Parameter]
    public bool AutoLabel { get; set; } = true;

    [Parameter]
    public bool EnableVerticalLine { get; set; } = false;
    
    private RenderFragment? _tooltipContent;
    private RenderFragment? _requiredLabelContent;
    private string? _fieldName;

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Dense = true;
        HideDetails = "auto";
        Outlined = true;
        MenuProps = props =>
        {
            props.OffsetY = true;
        };

        await base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        Class ??= "";

        if (Class.Contains("s-select") is false)
        {
            Class += " s-select";
        }
        if (Dense is true)
        {
            var (height, cssClass) = DenseSize.Resolve(Large, Medium, Small);
            MinHeight = height;
            Class = Class.WithClass(cssClass);
        }

        if (!string.IsNullOrWhiteSpace(Tooltip) && AppendOuterContent == default)
        {
            AppendOuterContent = _tooltipContent ??= RenderFragments.GenHelpIcon(Tooltip);
        }

        if (Required && LabelContent == default)
        {
            LabelContent = _requiredLabelContent ??= RenderFragments.GenRequiredLabel(Label);
        }

        if (string.IsNullOrEmpty(Label) is true && AutoLabel && ValueExpression is not null)
        {
            var fieldName = ValueExpression.GetFieldName();
            Label = I18n.T(fieldName);
        }

        if (EnableVerticalLine)
        {
            Class += " select_vertical_line";
        }
    }
}
