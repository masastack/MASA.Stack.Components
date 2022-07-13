// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public partial class RelativeTime
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
                dateTime = DateTime.Now.AddMinutes(-15);
                break;
            case RelativeTimeTypes.ThirtyMinutes:
                dateTime = DateTime.Now.AddMinutes(-30);
                break;
            case RelativeTimeTypes.OneHour:
                dateTime = DateTime.Now.AddHours(-1);
                break;
            case RelativeTimeTypes.TwoHour:
                dateTime = DateTime.Now.AddHours(-2);
                break;
            case RelativeTimeTypes.TwelveHour:
                dateTime = DateTime.Now.AddHours(-12);
                break;
            case RelativeTimeTypes.OneDay:
                dateTime = DateTime.Now.AddDays(-1);
                break;
            case RelativeTimeTypes.OneWeek:
                dateTime = DateTime.Now.AddDays(-7);
                break;
            case RelativeTimeTypes.OneMonth:
                dateTime = DateTime.Now.AddMonths(-1);
                break;
            default: break;
        }
        if (StartTimeChanged.HasDelegate) await StartTimeChanged.InvokeAsync(dateTime);
        else StartTime = dateTime;
        if (EndTimeChanged.HasDelegate) await EndTimeChanged.InvokeAsync(DateTime.Now);
        else EndTime = DateTime.Now;
    }

    public string ConverText(RelativeTimeTypes type)
    {
        return T(type.ToString() + " Ago");
    }
}
