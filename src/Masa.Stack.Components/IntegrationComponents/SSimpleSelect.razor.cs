// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public partial class SSimpleSelect<TValue>
{
    [Parameter]
    public TValue? Value { get; set; }

    [Parameter]
    public EventCallback<TValue?> ValueChanged { get; set; }

    [Parameter]
    public string? DefaultText { get; set; }

    [Parameter]
    public string Style { get; set; } = "";

    [Parameter]
    public string Class { get; set; } = "";

    public string Text => ValueTexts.FirstOrDefault(vt => vt.value?.Equals(Value) is true).text ?? DefaultText ?? T("Please select");

    [Parameter]
    public virtual List<(TValue value, string text)> ValueTexts { get; set; } = new();

    public bool MenuState { get; set; }

    private string Icon => MenuState ? "mdi-menu-up" : "mdi-menu-down";

    public async Task UpdateValueAsync(TValue? value)
    {
        if (ValueChanged.HasDelegate) await ValueChanged.InvokeAsync(value);
        else Value = value;
    }
}
