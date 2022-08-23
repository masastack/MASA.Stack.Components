// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public partial class SRelativeTime
{
    [Parameter]
    public string Style { get; set; } = "";

    [Parameter]
    public string Class { get; set; } = "";

    [Parameter]
    public DateTime? StartTime { get; set; }

    [Parameter]
    public EventCallback<DateTime?> StartTimeChanged { get; set; }

    [Parameter]
    public DateTime? EndTime { get; set; }

    [Parameter]
    public EventCallback<DateTime?> EndTimeChanged { get; set; }

    [Parameter]
    public TimeSpan OutputTimezoneOffset { get; set; } = TimeSpan.Zero;

    public RelativeTimeTypes RelativeTimeType
    {
        get
        {
            if (StartTime is not null && EndTime is not null)
            {
                var timeSpan = EndTime.Value.AddSeconds(-EndTime.Value.Second).Subtract(StartTime.Value.AddSeconds(-StartTime.Value.Second));
                var minutes = (int)timeSpan.TotalMinutes;
                return (minutes) switch
                {
                    15 => RelativeTimeTypes.FifteenMinutes,
                    30 => RelativeTimeTypes.ThirtyMinutes,
                    60 => RelativeTimeTypes.OneHour,
                    120 => RelativeTimeTypes.TwoHour,
                    720 => RelativeTimeTypes.TwelveHour,
                    1440 => RelativeTimeTypes.OneDay,
                    10080 => RelativeTimeTypes.OneWeek,
                    43200 => RelativeTimeTypes.OneMonth,
                    _ => default
                };
            }
            return default;
        }
    }

    public async Task UpdateValueAsync(RelativeTimeTypes type)
    {
        DateTime? dateTime = default;
        switch (type)
        {
            case RelativeTimeTypes.FifteenMinutes:
                dateTime = DateTime.UtcNow.AddMinutes(-15);
                break;
            case RelativeTimeTypes.ThirtyMinutes:
                dateTime = DateTime.UtcNow.AddMinutes(-30);
                break;
            case RelativeTimeTypes.OneHour:
                dateTime = DateTime.UtcNow.AddHours(-1);
                break;
            case RelativeTimeTypes.TwoHour:
                dateTime = DateTime.UtcNow.AddHours(-2);
                break;
            case RelativeTimeTypes.TwelveHour:
                dateTime = DateTime.UtcNow.AddHours(-12);
                break;
            case RelativeTimeTypes.OneDay:
                dateTime = DateTime.UtcNow.AddDays(-1);
                break;
            case RelativeTimeTypes.OneWeek:
                dateTime = DateTime.UtcNow.AddDays(-7);
                break;
            case RelativeTimeTypes.OneMonth:
                dateTime = DateTime.UtcNow.AddMonths(-1);
                break;
            default: break;
        }
        var startDateTime = dateTime?.Add(OutputTimezoneOffset);
        var endDateTime = DateTime.UtcNow.Add(OutputTimezoneOffset);
                    
        if (StartTimeChanged.HasDelegate) await StartTimeChanged.InvokeAsync(startDateTime);
        else StartTime = startDateTime;
        if (EndTimeChanged.HasDelegate) await EndTimeChanged.InvokeAsync(endDateTime);
        else EndTime = endDateTime;
    }

    public string ConverText(RelativeTimeTypes type)
    {
        return T(type.ToString() + " Ago");
    }
}
