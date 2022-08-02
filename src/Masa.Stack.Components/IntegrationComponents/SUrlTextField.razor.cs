// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public partial class SUrlTextField : IDisposable
{
    public string? _protocol;

    public string Protocol
    {
        get => _protocol ??= "Https";
        set => _protocol = value;
    }

    [CascadingParameter]
    public EditContext? EditContext { get; set; }

    [Parameter]
    public string Value
    {
        get => $"{Protocol}://{TextValue}";
        set
        {
            if (value is not null)
            {
                if (value.StartsWith("Https://"))
                {
                    _protocol = "Https";
                    TextValue = value.TrimStart("Https://".ToCharArray());
                }
                else if (value.StartsWith("Http://"))
                {
                    _protocol = "Http";
                    TextValue = value.TrimStart("Http://".ToCharArray());
                }
            }
        }
    }

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<string>>? ValueExpression { get; set; }

    [Parameter]
    public string Label { get; set; } = "";

    public string TextValue { get; set; } = "";

    protected FieldIdentifier ValueIdentifier { get; set; }

    public async Task ValueTextUpdateAsync(string value)
    {
        value = $"{Protocol}://{value}";
        if (ValueChanged.HasDelegate) await ValueChanged.InvokeAsync(value);
        else Value = value;
    }

    public async Task ProtocolUpdateAsync(string protocol)
    {
        var value = $"{protocol}://{TextValue}";
        if (ValueChanged.HasDelegate) await ValueChanged.InvokeAsync(value);
        else Value = value;
    }

    public List<string> Items { get; set; } = new() { "Http", "Https" };

    public List<string> ErrorMessage { get; set; } = new();

    protected override void OnInitialized()
    {
        if (EditContext is not null && ValueExpression is not null)
        {
            ValueIdentifier = FieldIdentifier.Create(ValueExpression);
            EditContext.OnValidationStateChanged += HandleOnValidationStateChanged;
        }
    }

    protected virtual void HandleOnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        ErrorMessage = EditContext!.GetValidationMessages(ValueIdentifier).ToList();
        base.InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (EditContext != null)
        {
            EditContext.OnValidationStateChanged -= HandleOnValidationStateChanged;
        }
    }
}
