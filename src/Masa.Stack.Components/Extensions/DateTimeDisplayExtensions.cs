namespace Masa.Stack.Components.Extensions;

public static class DateTimeDisplayExtensions
{
    /// <summary>
    /// 安全的“多久之前”显示：
    /// 对默认值（0001-01-01）、未来时间（服务器与客户端时钟偏差）等 TimeAgo 库不支持的值做兜底，避免渲染抛异常。
    /// </summary>
    public static string ToTimeAgo(this DateTime dateTime, CultureInfo cultureInfo)
    {
        if (dateTime == default || dateTime.Year <= 1)
            return string.Empty;

        var local = dateTime.Kind == DateTimeKind.Local ? dateTime : dateTime.ToLocalTime();
        var now = DateTime.Now;

        // 未来时间（时钟偏差）按“刚刚”处理
        if (local > now)
            local = now;

        try
        {
            return TimeAgo.DateTimeExtensions.TimeAgo(local, now, cultureInfo);
        }
        catch (NotSupportedException)
        {
            return local.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    /// <inheritdoc cref="ToTimeAgo(DateTime, CultureInfo)"/>
    public static string ToTimeAgo(this DateTime? dateTime, CultureInfo cultureInfo)
    {
        return dateTime.HasValue ? dateTime.Value.ToTimeAgo(cultureInfo) : string.Empty;
    }
}
