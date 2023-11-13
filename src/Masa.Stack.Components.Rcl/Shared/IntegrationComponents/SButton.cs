﻿// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

using Masa.Stack.Components.Rcl.Shared.PureComponents;

namespace Masa.Stack.Components.Rcl.Shared.IntegrationComponents;

public class SButton : SAutoLoadingButton
{
    [Parameter]
    public bool Medium { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Color = "primary";
        await base.SetParametersAsync(parameters);
    }

    protected override void SetComponentClass()
    {
        base.SetComponentClass();

        CssProvider.Merge(delegate (CssBuilder cssBuilder)
        {
            cssBuilder.Add("btn");
            cssBuilder.AddFirstIf(("large-button", () => Large), ("medium-button", () => Medium), ("small-button", () => Small));
        });
    }
}