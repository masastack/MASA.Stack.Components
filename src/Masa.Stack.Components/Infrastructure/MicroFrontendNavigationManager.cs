// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components.Infrastructure;

public class MicroFrontendNavigationManager : NavigationManager
{
    public readonly string ProjectPrefix;
    private readonly NavigationManager _originalNavigationManager;

    public MicroFrontendNavigationManager(NavigationManager navigationManager, string projectPrefix)
    {
        _originalNavigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        ProjectPrefix = projectPrefix?.Trim('/') ?? throw new ArgumentNullException(nameof(projectPrefix));

        Initialize(_originalNavigationManager.BaseUri, _originalNavigationManager.Uri);
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        if (_microFrontend && !IsAbsoluteUrl(uri) && !uri.StartsWith(ProjectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            uri = $"/{ProjectPrefix}/{uri.TrimStart('/')}";
        }
        _originalNavigationManager.NavigateTo(uri, forceLoad);
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
