// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public class DefaultButton : MButton
{
    [Parameter]
    public bool Medium { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if(Class is null || Class.Contains("primary") is false)
        {
            Class += " primary";
        }
    }

    protected override void SetComponentClass()
    {
        base.SetComponentClass();

        CssProvider.Merge(delegate (CssBuilder cssBuilder) {
            cssBuilder.AddFirstIf(("large-button", () => Large), ("medium-button", () => Medium), ("small-button", () => Small));
        });
    }
}