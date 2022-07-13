﻿namespace Masa.Stack.Components;

public partial class DateTimePicker
{
    private static readonly int[] _hours = Enumerable.Range(0, 24).ToArray();
    private static readonly int[] _minutes = Enumerable.Range(0, 60).ToArray();
    private static readonly int[] _seconds = Enumerable.Range(0, 60).ToArray();

    [Inject] public IPopupService PopupService { get; set; }
    [Parameter] public DateTime? Max { get; set; }
    [Parameter] public DateTime? Min { get; set; }
    [Parameter] public bool NoTitle { get; set; }
    [Parameter] public DateTime? Value { get; set; }
    [Parameter] public EventCallback<DateTime?> ValueChanged { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private DateOnly? Date => Value is null ? null : DateOnly.FromDateTime(Value.Value);
    private TimeOnly Time => Value is null ? new(GetHours()[0], GetMinutes()[0], GetSeconds()[0]) : TimeOnly.FromDateTime(Value.Value);

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);
        if (Max is not null && Min is not null && Max < Min)
        {
            await PopupService.AlertAsync(T("The maximum time cannot be less than the minimum time"), AlertTypes.Error);
            Max = null;
        }
    }

    private int[] GetHours()
    {
        if (Min is null && Max is null) return _hours;
        else
        {
            var hours = _hours;
            if (Min is not null && Value is not null && Min.Value.Date >= Value.Value.Date) hours = hours.Where(h => h >= Min.Value.Hour).ToArray();
            else if (Max is not null && Value is not null && Max.Value.Date <= Value.Value.Date) hours = hours.Where(h => h <= Max.Value.Hour).ToArray();
            return hours;
        }
    }

    private int[] GetMinutes()
    {
        if (Min is null && Max is null) return _minutes;
        {
            if (Min is not null && Value is not null && Min.Value.Date >= Value.Value.Date) return _minutes.Where(h => h >= Min.Value.Minute).ToArray();
            else if (Max is not null && Value is not null && Max.Value.Date <= Value.Value.Date) return _minutes.Where(h => h <= Max.Value.Minute).ToArray();
            return _minutes;
        }
    }

    private int[] GetSeconds()
    {
        if (Min is null && Max is null) return _seconds;
        {
            if (Min is not null && Value is not null && Min.Value.Date >= Value.Value.Date) return _seconds.Where(h => h >= Min.Value.Second).ToArray();
            else if (Max is not null && Value is not null && Max.Value.Date <= Value.Value.Date) return _seconds.Where(h => h <= Max.Value.Second).ToArray();
            else return _seconds;
        }
    }

    private bool GetNowClickState()
    {
        if (Min is not null) return DateTime.Now < Min;
        else if (Max is not null) return DateTime.Now > Max;
        else return false;
    }

    private DateOnly? GetMinDateOnly()
    {
        if (Min is not null)
        {
            if (Value is null) return DateOnly.FromDateTime(Min.Value);
            else if (Min.Value.TimeOfDay < Value.Value.TimeOfDay) return DateOnly.FromDateTime(Min.Value.AddDays(1));
            else return DateOnly.FromDateTime(Min.Value);
        }
        else return null;
    }

    private DateOnly? GetMaxDateOnly()
    {
        if (Max is not null)
        {
            if (Value is null) return DateOnly.FromDateTime(Max.Value);
            else if (Max.Value.TimeOfDay < Value.Value.TimeOfDay) return DateOnly.FromDateTime(Max.Value.AddDays(-1));
            else return DateOnly.FromDateTime(Max.Value);
        }
        return null;
    }

    private async Task DateChangedAsync(DateOnly? date)
    {
        await UpdateValueAsync(date?.ToDateTime(Time));
    }

    private async Task HourChangedAsync(int hour)
    {
        var time = new TimeOnly(hour, Time.Minute, Time.Second);
        await UpdateValueAsync(time);
    }

    private async Task MinuteChangedAsync(int minute)
    {
        var time = new TimeOnly(Time.Hour, minute, Time.Second);
        await UpdateValueAsync(time);
    }

    private async Task SecondChangedAsync(int second)
    {
        var time = new TimeOnly(Time.Hour, Time.Minute, second);
        await UpdateValueAsync(time);
    }

    private async Task UpdateValueAsync(DateTime? dateTime)
    {
        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(dateTime);
        }
        else
        {
            Value = dateTime;
        }
    }

    private async Task UpdateValueAsync(TimeOnly time)
    {
        DateTime? dateTime = default;
        if (Date is null)
        {
            var now = DateTime.Now;
            if (Min is not null)
            {
                if (Min < now) dateTime = DateOnly.FromDateTime(now).ToDateTime(time);
                else dateTime = DateOnly.FromDateTime(Min.Value).ToDateTime(time);
            }
            else if (Max is not null)
            {
                if (Max < now) dateTime = DateOnly.FromDateTime(Max.Value).ToDateTime(time);
                else dateTime = DateOnly.FromDateTime(now).ToDateTime(time);
            }
        }
        else dateTime = Date.Value.ToDateTime(time);

        await UpdateValueAsync(dateTime);
    }

    private async Task OnNowAsync()
    {
        await UpdateValueAsync(DateTime.Now);
    }

    private async Task OnResetAsync()
    {
        await UpdateValueAsync(null);
    }  
}