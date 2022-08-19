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
    public bool Local { get; set; } = true;

    [Parameter]
    public DateTime? Value
    {
        get => _value;
        set
        {
            if (Local)
                _value = value?.Add(JsInitVariables.TimezoneOffset);
            else
                _value = value;
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
