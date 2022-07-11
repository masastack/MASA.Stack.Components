namespace Masa.Stack.Components;

public partial class DateTimePicker
{
    private static readonly int[] _hours = Enumerable.Range(0, 24).ToArray();
    private static readonly int[] _minutes = Enumerable.Range(0, 60).ToArray();
    private static readonly int[] _seconds = Enumerable.Range(0, 60).ToArray();

    [Parameter] public DateTime? Max { get; set; }
    [Parameter] public DateTime? Min { get; set; }
    [Parameter] public bool NoTitle { get; set; }
    [Parameter] public DateTime? Value { get; set; }
    [Parameter] public EventCallback<DateTime?> ValueChanged { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private DateOnly? Date => Value is null ? null : DateOnly.FromDateTime(Value.Value);
    private TimeOnly Time => Value is null ? default : TimeOnly.FromDateTime(Value.Value);

    private int[] GetHours()
    {
        if (Min is not null) return _hours.Where(h => h >= Min.Value.Hour).ToArray();
        else if (Max is not null) return _hours.Where(h => h <= Max.Value.Hour).ToArray();
        else return _hours;
    }

    private int[] GetMinutes()
    {
        if (Min is not null) return _minutes.Where(h => h >= Min.Value.Minute).ToArray();
        else if (Max is not null) return _minutes.Where(h => h <= Max.Value.Minute).ToArray();
        else return _minutes;
    }

    private int[] GetSeconds()
    {
        if (Min is not null) return _seconds.Where(h => h >= Min.Value.Second).ToArray();
        else if (Max is not null) return _seconds.Where(h => h <= Max.Value.Second).ToArray();
        else return _seconds;
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
        if (Date is null)
            await UpdateValueAsync(DateOnly.FromDateTime(DateTime.Now).ToDateTime(time));
        else
            await UpdateValueAsync(Date.Value.ToDateTime(time));
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