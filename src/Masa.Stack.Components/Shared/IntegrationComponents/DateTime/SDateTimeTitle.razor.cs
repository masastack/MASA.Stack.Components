// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public partial class SDateTimeTitle
{
    DateTime? _value;

    [Parameter]
    public bool DateTime { get; set; }

    [Parameter]
    public bool Date { get; set; }

    [Parameter]
    public bool Time { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public TimeSpan DisplayTimezoneOffset { get; set; } = JsInitVariables.TimezoneOffset;

    [Parameter]
    public DateTime? Value
    {
        get => _value;
        set
        {
            _value = value?.Add(DisplayTimezoneOffset);       
        }
    }

    protected override void OnParametersSet()
    {
        if ((DateTime, Date, Time) == (false, false, false))
        {
            Date = true;
            Time = true;
        }
    }
}
