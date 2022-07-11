// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public partial class DateTimeRangePicker
{
    [Inject]
    public IPopupService PopupService { get; set; } = default!;

    [Parameter]
    public string Class { get; set; } = "";

    [Parameter]
    public string Style { get; set; } = "";

    [Parameter]
    public DateTime? StartTime { get; set; }

    [Parameter]
    public EventCallback<DateTime?> StartTimeChanged { get; set; }

    [Parameter]
    public DateTime? EndTime { get; set; }

    [Parameter]
    public EventCallback<DateTime?> EndTimeChanged { get; set; }

    private bool StartTimeVisible { get; set; }

    private bool EndTimeVisible { get; set; }

    private async Task UpdateStartTimeAsync(DateTime? dateTime)
    {
        if (dateTime > EndTime) await PopupService.AlertAsync(T("Start time cannot be greater than end time"), AlertTypes.Warning);
        else
        {
            StartTime = dateTime;
            if (StartTimeChanged.HasDelegate) await StartTimeChanged.InvokeAsync(dateTime);
        }
    }

    private async Task UpdateEndTimeAsync(DateTime? dateTime)
    {
        if (dateTime < StartTime) await PopupService.AlertAsync(T("End time cannot be less than start time"), AlertTypes.Warning);
        else
        {
            EndTime = dateTime;
            if (StartTimeChanged.HasDelegate) await EndTimeChanged.InvokeAsync(dateTime);
        }
    }
}

