// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public class DefaultDataTable<TItem> : MDataTable<TItem>
{
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        HideDefaultFooter = true;
        await base.SetParametersAsync(parameters);
        Class ??= "";
        Class += "table-border-none";
    }
}

