﻿namespace Masa.Stack.Components.Rcl.Shared.IntegrationComponents;

public class SListItem : MListItem
{
    [Parameter]
    public bool Medium { get; set; }

    protected override void SetComponentClass()
    {
        base.SetComponentClass();

        CssProvider.Merge(css => { css.AddIf("m-list-item--medium", () => Medium); });
    }
}
