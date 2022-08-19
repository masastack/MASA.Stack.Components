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
    public DateTimeKind OutputKind { get; set; } = DateTimeKind.Utc;

    private DateTime? InternalEndTime { get; set; }

    private bool StartTimeVisible { get; set; }

    private bool EndTimeVisible { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);
        InternalStartTime = StartTime;
        InternalEndTime = EndTime;
        if (StartTime?.Kind is DateTimeKind.Utc)
            StartTime = StartTime.Value.Add(JsInitVariables.TimezoneOffset);
        if (EndTime?.Kind is DateTimeKind.Utc)
            EndTime = EndTime.Value.Add(JsInitVariables.TimezoneOffset);
    }

    private async Task UpdateStartTimeAsync()
    {
        StartTimeVisible = false;
        var startTime = InternalStartTime;
        if (startTime?.Kind is DateTimeKind.Utc) startTime = startTime.Value.Add(JsInitVariables.TimezoneOffset);
        if (startTime > EndTime) await PopupService.AlertAsync(T("Start time cannot be greater than end time"), AlertTypes.Warning);
        else
        {
            if (StartTimeChanged.HasDelegate) await StartTimeChanged.InvokeAsync(InternalStartTime);
            else StartTime = InternalStartTime;
        }
    }

    private async Task UpdateEndTimeAsync()
    {
        EndTimeVisible = false;
        var endTime = InternalEndTime;
        if (endTime?.Kind is DateTimeKind.Utc) endTime = endTime.Value.Add(JsInitVariables.TimezoneOffset);
        if (endTime < StartTime) await PopupService.AlertAsync(T("End time cannot be less than start time"), AlertTypes.Warning);
        else
        {
            if (EndTimeChanged.HasDelegate) await EndTimeChanged.InvokeAsync(InternalEndTime);
            else EndTime = InternalEndTime;
        }
    }
}

