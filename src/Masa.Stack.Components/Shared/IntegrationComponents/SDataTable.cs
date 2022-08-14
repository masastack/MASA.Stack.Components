// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public class SDataTable<TItem> : MDataTable<TItem>
{
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        HideDefaultFooter = true;
        FixedHeader = true;
        await base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Class ??= "";
        if (Class.Contains("table-border-none") is false)
            Class += " table-border-none";
        if(Dense && Class.Contains("dense") is false)
            Class += " dense";
    }
}

