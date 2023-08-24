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

        if (Required && LabelContent == default)
        {
            LabelContent = builder =>
            {
                builder.OpenElement(0, "label");
                builder.AddAttribute(1, "class", "red--text mr-1");
                builder.AddContent(2, "*");
                builder.CloseElement();
                builder.AddContent(3, Label);
            };
        }

        if (string.IsNullOrEmpty(Label) is true && AutoLabel && ValueExpression is not null)
        {
            var accessorBody = ValueExpression.Body;

            if (accessorBody is UnaryExpression unaryExpression
                && unaryExpression.NodeType == ExpressionType.Convert
                && unaryExpression.Type == typeof(object))
            {
                accessorBody = unaryExpression.Operand;
            }

            var fieldName = (accessorBody as MemberExpression)!.Member.Name;
            Label = I18n.T(fieldName);
        }
    }
}
