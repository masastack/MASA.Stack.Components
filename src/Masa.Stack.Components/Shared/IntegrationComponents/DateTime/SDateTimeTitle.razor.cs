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
    public DateTimeKind Kind { get; set; } = DateTimeKind.Local;

    [Parameter]
    public DateTime? Value
    {
        get => _value;
        set
        {
            if(value is not null)
            {
                var dateTime = value.Value;
                if (Kind is DateTimeKind.Utc)
                {
                    _value = dateTime.ToUniversalTime();
                }                    
                else if(Kind is DateTimeKind.Local)
                {
                    if (dateTime.Kind is not DateTimeKind.Local)
                        _value = dateTime.Add(JsInitVariables.TimezoneOffset);
                    else
                    {
                        _value = dateTime.ToUniversalTime().Add(JsInitVariables.TimezoneOffset);
                    }
                }
                else
                    _value = dateTime;
            }
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
