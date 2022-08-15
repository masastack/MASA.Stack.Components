// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public class SDataTable<TItem> : MDataTable<TItem>
{
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        HideDefaultFooter = true;
        FixedHeader = true;

        ItemColContent = item =>
        {
            void Content(RenderTreeBuilder builder)
            {
                builder.OpenComponent(0, typeof(SItemCol));
                builder.AddAttribute(1, nameof(SItemCol.Value), item.Value);
                builder.CloseComponent();
            }

            return Content;
        };

        await base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Class ??= "";
        if (Class.Contains("table-border-none") is false)
            Class += " table-border-none";
        if (Dense && Class.Contains("dense") is false)
            Class += " dense";
    }
}
