﻿namespace Masa.Stack.Components;

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

        if (!string.IsNullOrWhiteSpace(Tooltip) && AppendOuterContent == default)
        {
            AppendOuterContent = builder =>
            {
                builder.OpenComponent<SIcon>(0);
                builder.AddAttribute(1, "Tooltip", Tooltip);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(cb => cb.AddContent(3, "mdi-help-circle-outline")));
                builder.CloseComponent();
            };
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

        if (EnableVerticalLine)
        {
            Class += " select_vertical_line";
        }

        Required = false; // disable required feature from base component in S[Component]
    }
}
