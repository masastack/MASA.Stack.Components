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
        if (_microFrontend && !forceLoad && !uri.StartsWith(ProjectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            uri = $"/{ProjectPrefix}/{uri.TrimStart('/')}";
        }
        _originalNavigationManager.NavigateTo(uri, forceLoad);
    }

    private bool _microFrontend
    {
        get
        {
            return !string.IsNullOrEmpty(ProjectPrefix) && ProjectPrefix != "/";
        }
    }
}
