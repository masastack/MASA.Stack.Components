namespace Masa.Stack.Components;

internal static class NavigationUrlHelper
{
    public static string BuildHref(string? url, string projectPrefix, Uri currentUri)
    {
        if (string.IsNullOrEmpty(url))
        {
            return projectPrefix;
        }
        
        if (TryBuildHrefFromAbsoluteUrl(url, projectPrefix, currentUri, out var absoluteHref))
        {
            return absoluteHref;
        }

        return BuildHrefFromRelativeUrl(url, projectPrefix);
    }

    private static bool TryBuildHrefFromAbsoluteUrl(string url, string projectPrefix, Uri currentUri, out string href)
    {
        href = string.Empty;

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return false;
        }

        if (!IsSameOrigin(absoluteUri, currentUri))
        {
            href = url;
            return true;
        }

        href = EnsureProjectPrefix(absoluteUri.PathAndQuery + absoluteUri.Fragment, projectPrefix);
        return true;
    }

    private static bool IsSameOrigin(Uri targetUri, Uri currentUri)
    {
        return string.Equals(targetUri.Host, currentUri.Host, StringComparison.OrdinalIgnoreCase) &&
               targetUri.Port == currentUri.Port &&
               string.Equals(targetUri.Scheme, currentUri.Scheme, StringComparison.OrdinalIgnoreCase);
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
