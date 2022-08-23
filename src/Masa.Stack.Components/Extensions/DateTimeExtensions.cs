namespace Masa.Stack.Components.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ToLocalKind(this DateTime datetime)
    {
        return DateTime.SpecifyKind(datetime, DateTimeKind.Local);
    }

    public static DateTime ToUtcKind(this DateTime datetime)
    {
        return DateTime.SpecifyKind(datetime, DateTimeKind.Utc);
    }

    public static DateTime ToUnspecifiedKind(this DateTime datetime)
    {
        return DateTime.SpecifyKind(datetime, DateTimeKind.Unspecified);
    }
}
