using Masa.Stack.Components.Extensions;

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
    public bool AutoLabel { get; set; } = true;

    private RenderFragment? _requiredLabelContent;
    private string? _fieldName;

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Dense = true;
        HideDetails = "auto";
        Outlined = true;
        HideSelected = true;
        Color = "primary";
        Style = "";
        Class = "";
        BackgroundColor = "fill";

        await base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // keep the background radius consistent with the 8px outline
        Style = Style.WithDefaultBorderRadius();

        Class ??= "";
        if (Dense is true)
        {
            var (height, cssClass) = DenseSize.Resolve(Large, Medium, Small);
            Height = height;
            Class = Class.WithClass(cssClass);
        }

        if (Required && LabelContent == default)
        {
            LabelContent = _requiredLabelContent ??= RenderFragments.GenRequiredLabel(Label);
        }

        if (string.IsNullOrEmpty(Label) is true && AutoLabel && ValueExpression is not null)
        {
            var fieldName = _fieldName ?? ValueExpression.GetFieldName();
            Label = I18n.T(fieldName);
        }
    }
}
