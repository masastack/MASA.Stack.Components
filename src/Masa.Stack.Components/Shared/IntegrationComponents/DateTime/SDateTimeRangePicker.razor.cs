// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public partial class SDateTimeRangePicker
{
    [Inject]
    public JsInitVariables JsInitVariables { get; set; } = default!;

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
    public TimeSpan ValueTimezoneOffset { get; set; } = TimeSpan.Zero;

    [Parameter]
    public TimeSpan DisplayTimezoneOffset { get; set; }

    [Parameter]
    public EventCallback OnChange { get; set; }

    private DateTime? InternalEndTime { get; set; }

    private bool StartTimeVisible { get; set; }

    private bool EndTimeVisible { get; set; }

    private string _datetimeStartTextCss = "regular3--text";

    private string _datetimeEndTextCss = "regular3--text";

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        DisplayTimezoneOffset = JsInitVariables.TimezoneOffset;
        await base.SetParametersAsync(parameters);
        InternalStartTime = StartTime;
        InternalEndTime = EndTime;
    }

    private async Task UpdateStartTimeAsync()
    {
        StartTimeVisible = false;
        if (InternalStartTime > EndTime) await PopupService.EnqueueSnackbarAsync(T("Start time cannot be greater than end time"), AlertTypes.Warning);
        else
        {
            _datetimeStartTextCss = InternalStartTime is null ? "regular3--text" : "regular--text";

            if (StartTimeChanged.HasDelegate) await StartTimeChanged.InvokeAsync(InternalStartTime);
            else StartTime = InternalStartTime;
            if (OnChange.HasDelegate) await OnChange.InvokeAsync();
        }
    }

    private async Task UpdateEndTimeAsync()
    {
        EndTimeVisible = false;
        if (InternalEndTime < StartTime) await PopupService.EnqueueSnackbarAsync(T("End time cannot be less than start time"), AlertTypes.Warning);
        else
        {
            _datetimeEndTextCss = InternalEndTime is null ? "regular3--text" : "regular--text";

            if (EndTimeChanged.HasDelegate) await EndTimeChanged.InvokeAsync(InternalEndTime);
            else EndTime = InternalEndTime;
            if (OnChange.HasDelegate) await OnChange.InvokeAsync();
        }
    }
}

