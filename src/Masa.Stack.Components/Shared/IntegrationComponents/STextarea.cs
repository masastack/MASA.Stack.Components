﻿namespace Masa.Stack.Components;

public class STextarea : MTextarea
{
    [Parameter]
    public bool Required { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        HideDetails = "auto";
        Outlined = true;

        await base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
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
    }
}
