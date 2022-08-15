// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public partial class SDateRangePicker
{
    [Inject]
    public IPopupService PopupService { get; set; } = default!;

    [Parameter]
    public string Class { get; set; } = "";

    [Parameter]
    public string Style { get; set; } = "";

    [Parameter]
    public DateOnly? StartTime { get; set; }

    [Parameter]
    public EventCallback<DateOnly?> StartTimeChanged { get; set; }

    [Parameter]
    public DateOnly? EndTime { get; set; }

    [Parameter]
    public EventCallback<DateOnly?> EndTimeChanged { get; set; }

    private bool StartTimeVisible { get; set; }

    private bool EndTimeVisible { get; set; }

    private async Task UpdateStartTimeAsync(DateOnly? dateTime)
    {
        if (dateTime > EndTime) await PopupService.AlertAsync(T("Start time cannot be greater than end time"), AlertTypes.Warning);
        else
        {
            StartTime = dateTime;
            if (StartTimeChanged.HasDelegate) await StartTimeChanged.InvokeAsync(dateTime);
        }
    }

    private async Task UpdateEndTimeAsync(DateOnly? dateTime)
    {
        if (dateTime < StartTime) await PopupService.AlertAsync(T("End time cannot be less than start time"), AlertTypes.Warning);
        else
        {
            EndTime = dateTime;
            if (StartTimeChanged.HasDelegate) await EndTimeChanged.InvokeAsync(dateTime);
        }
    }
}

