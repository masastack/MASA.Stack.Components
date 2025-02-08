// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components.Infrastructure;

public class MicroFrontendNavigationManager : NavigationManager
{
    public readonly string ProjectPrefix;

    public readonly NavigationManager OriginalNavigationManager;

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

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        Console.WriteLine($"NavigateToCore: {uri}");
        if (_microFrontend && !IsAbsoluteUrl(uri) && uri.StartsWith("/") && !uri.StartsWith(ProjectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            uri = $"{ProjectPrefix}{uri.TrimStart("/")}";
        }
        OriginalNavigationManager.NavigateTo(uri, forceLoad);
    }

    private bool IsAbsoluteUrl(string url)
    {
        // Try parsing the URL and check if it has a scheme (e.g., http, https)
        Uri result;
        return System.Uri.TryCreate(url, UriKind.Absolute, out result) && (result.Scheme == System.Uri.UriSchemeHttp || result.Scheme == System.Uri.UriSchemeHttps);
    }

    private bool _microFrontend
    {
        get
        {
            return !string.IsNullOrEmpty(ProjectPrefix) && ProjectPrefix != "/";
        }
    }
}
