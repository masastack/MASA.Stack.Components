namespace Masa.Stack.Components.OpenTelemetry.Blazor;

public static class MasaBlazorActivityContent
{
    private static readonly Lazy<HttpClient> _lazyHttpClient = new(() => new HttpClient());

    public static Activity? CurrentActivity { get; set; }

    public static string ExternalIp { get; set; } = default!;

    public static string UserAgent { get; set; } = default!;

    public static object? BlazorPage { get; set; } = default!;

    private static Dictionary<string, object> _blazorPages = [];

    internal static string? GetIpUrl { get; set; }

    public static void AddPage(string url, object page)
    {
        var key = url?.ToLower();
        if (string.IsNullOrEmpty(key) || page == null || _blazorPages.ContainsKey(key)) return;
        _blazorPages.Add(key, page);
    }

    public static object? GetPage(string url)
    {
        var key = url?.ToLower();
        if (!string.IsNullOrEmpty(key) && _blazorPages.TryGetValue(key, out var page))
            return page;
        return default;
    }

    public static void RemovePage(string url)
    {
        var key = url?.ToLower();
        if (string.IsNullOrEmpty(key) || !_blazorPages.ContainsKey(key))
            return;
        _blazorPages.Remove(key);
    }

    public static void RemovePage()
    {
        var item = _blazorPages.FirstOrDefault(item => item.Value == BlazorPage);
        if (item.Key != null)
            _blazorPages.Remove(item.Key);
    }

    public static IDisposable? GetLogScope(string? url = default, object? page = default)
    {
        if (page == null && string.IsNullOrEmpty(url))
            return default;
        if (page == null)
            page = GetPage(url!);

        if (page == null)
            return default;

        var method = page.GetType().GetMethod("GetLogScope");
        if (method != null) return method.Invoke(page, null) as IDisposable;
        return default;
    }

    public static IDictionary<string, object>? GetLogScopeValues(string? url = default, object? page = default)
    {
        if (page == null && string.IsNullOrEmpty(url))
            return default;
        if (page == null)
            page = GetPage(url!);

        if (page == null)
            return default;

        var method = page.GetType().GetMethod("GetLogScopeValues");
        if (method != null) return method.Invoke(page, null) as IDictionary<string, object>;
        return default;
    }

    public static async Task<string> GetIpAsync()
    {
        if (string.IsNullOrEmpty(GetIpUrl)) return string.Empty;
        if (!string.IsNullOrEmpty(ExternalIp)) return ExternalIp;
        ExternalIp = await _lazyHttpClient.Value.GetStringAsync(GetIpUrl, default);
        return ExternalIp;
    }
}