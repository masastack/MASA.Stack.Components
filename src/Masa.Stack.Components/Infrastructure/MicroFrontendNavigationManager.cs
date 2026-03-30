// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components.Infrastructure;

public class MicroFrontendNavigationManager : NavigationManager
{
    public readonly string ProjectPrefix;

    public readonly NavigationManager OriginalNavigationManager;

    /// <summary>
    /// 与注册时 <c>microFrontend: true</c> 一致：<see cref="ProjectPrefix"/> 为 <c>/{project}/</c>；
    /// 独立应用为 <c>false</c>（前缀为 <c>/</c>）。
    /// </summary>
    public bool IsMicroFrontend =>
        !string.IsNullOrEmpty(ProjectPrefix) && !string.Equals(ProjectPrefix, "/", StringComparison.Ordinal);

    public new event EventHandler<LocationChangedEventArgs> LocationChanged
    {
        add => OriginalNavigationManager.LocationChanged += value;
        remove => OriginalNavigationManager.LocationChanged -= value;
    }

    public new string BaseUri => OriginalNavigationManager.BaseUri;

    public new string Uri => OriginalNavigationManager.Uri;

    public MicroFrontendNavigationManager(NavigationManager navigationManager, string projectPrefix)
    {
        OriginalNavigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        ProjectPrefix = projectPrefix ?? throw new ArgumentNullException(nameof(projectPrefix));
        Initialize(OriginalNavigationManager.BaseUri, OriginalNavigationManager.Uri);
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        if (IsMicroFrontend && !IsAbsoluteUrl(uri) && uri.StartsWith("/") && !uri.StartsWith(ProjectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            uri = $"{ProjectPrefix}{uri.TrimStart("/")}";
        }

        OriginalNavigationManager.NavigateTo(uri, options.ForceLoad, options.ReplaceHistoryEntry);
    }

    private bool IsAbsoluteUrl(string url)
    {
        // Try parsing the URL and check if it has a scheme (e.g., http, https)
        Uri result;
        return System.Uri.TryCreate(url, UriKind.Absolute, out result) && (result.Scheme == System.Uri.UriSchemeHttp || result.Scheme == System.Uri.UriSchemeHttps);
    }

}
