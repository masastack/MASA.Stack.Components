// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public partial class SDateTimeRangePicker
{
    [Parameter]
    public string Class { get; set; } = "";

    [Parameter]
    public string Style { get; set; } = "";

    [Parameter]
    public DateTime? StartTime { get; set; }

    [Parameter]
    public EventCallback<DateTime?> StartTimeChanged { get; set; }

    private DateTime? InternalStartTime { get; set; }

    [Parameter]
    public DateTime? EndTime { get; set; }

    [Parameter]
    public EventCallback<DateTime?> EndTimeChanged { get; set; }

    [Parameter]
    public TimeSpan OutputTimezoneOffset { get; set; } = TimeSpan.Zero;

    [Parameter]
    public TimeSpan ShowTimezoneOffset { get; set; } = JsInitVariables.TimezoneOffset;

    private DateTime? InternalEndTime { get; set; }

    private bool StartTimeVisible { get; set; }

    private bool EndTimeVisible { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);
        InternalStartTime = StartTime;
        InternalEndTime = EndTime;
    }

    private async Task UpdateStartTimeAsync()
    {
        StartTimeVisible = false;        
        if (InternalStartTime > EndTime) await PopupService.AlertAsync(T("Start time cannot be greater than end time"), AlertTypes.Warning);
        else
        {
            if (StartTimeChanged.HasDelegate) await StartTimeChanged.InvokeAsync(InternalStartTime);
            else StartTime = InternalStartTime;
        }
    }

    private async Task UpdateEndTimeAsync()
    {
        EndTimeVisible = false;    
        if (InternalEndTime < StartTime) await PopupService.AlertAsync(T("End time cannot be less than start time"), AlertTypes.Warning);
        else
        {
            if (EndTimeChanged.HasDelegate) await EndTimeChanged.InvokeAsync(InternalEndTime);
            else EndTime = InternalEndTime;
        }
    }
}

