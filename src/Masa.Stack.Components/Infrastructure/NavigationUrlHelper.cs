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

    /// <summary>
    /// 为菜单项生成 <c>MatchPattern</c>，让左侧菜单的选中态在微前端下也能生效。
    /// <para>
    /// MASA 的 <c>MListItem</c> 默认用 <c>Href</c> 去匹配「相对 Blazor 基地址的路径」，微前端下基地址是 <c>/{project}/</c>，
    /// 当前路径为 <c>/job</c>；但 <see cref="BuildHref"/> 生成的 <c>Href</c> 带项目前缀（<c>/scheduler/job</c>），二者永远对不上。
    /// </para>
    /// <para>
    /// 因此在微前端且菜单未显式配置 <paramref name="matchPattern"/> 时，生成与 MASA 默认规则等价、并兼容可选项目前缀的正则；
    /// 单体应用（前缀为 <c>/</c>）、已配置 <paramref name="matchPattern"/>、绝对地址均原样返回，保持原有行为。
    /// </para>
    /// </summary>
    public static string? BuildMatchPattern(string? url, bool exact, string? matchPattern, string projectPrefix)
    {
        if (!string.IsNullOrWhiteSpace(matchPattern))
        {
            return matchPattern;
        }

        if (string.IsNullOrEmpty(url) ||
            string.IsNullOrEmpty(projectPrefix) ||
            string.Equals(projectPrefix, "/", StringComparison.Ordinal) ||
            url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return matchPattern;
        }

        var prefix = projectPrefix.TrimEnd('/');

        // 菜单 URL 可能已带前缀，先去掉，统一得到以 / 开头的项目内路径。
        var path = url.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase)
            ? url.Substring(prefix.Length)
            : url;
        if (!path.StartsWith("/"))
        {
            path = "/" + path;
        }

        // 与 IRoutable.MatchRoute 的默认规则保持一致：Exact 或根路径要求整段匹配，否则按前缀匹配。
        if (exact || path == "/")
        {
            path += "/?$";
        }

        // 前缀可选：Blazor 基地址是 /{project}/ 时路径不含前缀，基地址是 / 时路径含前缀，两种都要能匹配。
        return $"^(?:{Regex.Escape(prefix)})?{path}";
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
