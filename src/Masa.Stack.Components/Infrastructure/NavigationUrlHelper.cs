namespace Masa.Stack.Components;

internal static class NavigationUrlHelper
{
    /// <summary>
    /// 相对路径会按 <paramref name="projectPrefix"/> 补全；以 http:// 或 https:// 开头的菜单 URL 原样返回，由浏览器处理。
    /// </summary>
    public static string BuildHref(string? url, string projectPrefix)
    {
        if (string.IsNullOrEmpty(url))
        {
            return projectPrefix;
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        return BuildHrefFromRelativeUrl(url, projectPrefix);
    }

    private static string BuildHrefFromRelativeUrl(string url, string projectPrefix)
    {
        if (url.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        return EnsureProjectPrefix(url, projectPrefix);
    }

    private static string EnsureProjectPrefix(string url, string projectPrefix)
    {
        var prefix = projectPrefix.TrimEnd('/');
        if (url.StartsWith("/"))
        {
            return prefix + url;
        }

        return prefix + "/" + url;
    }
}
