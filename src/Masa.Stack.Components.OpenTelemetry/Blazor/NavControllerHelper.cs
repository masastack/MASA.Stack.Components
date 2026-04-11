namespace Masa.Stack.Components.OpenTelemetry.Blazor;

internal static class NavControllerHelper
{
    private static string? _currentUserId;
    private static string? _currentUserName;
    private static string? _currentTokenHash;

    public static void RefreshCurrentUserId(string? userId, string? name, string? tokenHash)
    {
        _currentUserId = userId;
        _currentUserName = name;
        _currentTokenHash = tokenHash;
    }

    private static void SetModule(Activity? activity)
    {
        if (activity == null)
            return;

        if (!string.IsNullOrEmpty(_currentUserId))
            activity.SetTag(MasaBlazorWasmConstants.SessionUserId, _currentUserId);
        if (!string.IsNullOrEmpty(_currentUserName))
            activity.SetTag(MasaBlazorWasmConstants.SessionUserName, _currentUserName);
        if (!string.IsNullOrEmpty(_currentTokenHash))
            activity.SetTag(MasaBlazorWasmConstants.BlazorPageSessionId, _currentTokenHash);

        if (RouteUtils.CurrentModule != null)
        {
            activity.SetTag(MasaBlazorWasmConstants.BlazorPageModuleName, RouteUtils.CurrentModule.Name);
            activity.SetTag(MasaBlazorWasmConstants.BlazorPageModuleCode, RouteUtils.CurrentModule.Code);
            activity.SetTag(MasaBlazorWasmConstants.BlazorPageModuleVersion, RouteUtils.CurrentModule.Version);
        }
    }

    #region 设置blazor trace

    public static Activity? StartPageActivity(ActivitySource ActivitySource, object page, NavigationManager NavigationManager, string prefix, string userAgent)
    {
        var activity = ActivitySource?.StartActivity(page.GetType().Name, ActivityKind.Server)!;
        if (activity == null) return default;
        var url = NavigationManager.Uri.Replace(NavigationManager.BaseUri, prefix);
        AfterLocationChangedAsync(url, page);
        activity.SetTag(MasaBlazorWasmConstants.HttpRequestSchema, "http");
        activity.SetTag(MasaBlazorWasmConstants.BlazorClientType, "wasm-blazor");
        activity.SetTag(MasaBlazorWasmConstants.HttpRequestUserAgent, userAgent);
        if (MasaBlazorActivityContent.CurrentActivity != null)
        {
            activity.SetTag(MasaBlazorWasmConstants.BlazorPageFromPath, MasaBlazorActivityContent.CurrentActivity.GetTagItem(MasaBlazorWasmConstants.BlazorPagePath));
            activity.SetTag(MasaBlazorWasmConstants.BlazorPageFromTitle, MasaBlazorActivityContent.CurrentActivity.GetTagItem(MasaBlazorWasmConstants.BlazorPageTitle));

            var fromModuleCode = MasaBlazorActivityContent.CurrentActivity.GetTagItem(MasaBlazorWasmConstants.BlazorPageModuleCode)?.ToString();
            if (!string.IsNullOrEmpty(fromModuleCode) && fromModuleCode != RouteUtils.CurrentModule?.Code)
            {
                activity.SetTag(MasaBlazorWasmConstants.BlazorPageModuleFromCode, MasaBlazorActivityContent.CurrentActivity.GetTagItem(MasaBlazorWasmConstants.BlazorPageModuleCode));
                activity.SetTag(MasaBlazorWasmConstants.BlazorPageModuleFromVersion, MasaBlazorActivityContent.CurrentActivity.GetTagItem(MasaBlazorWasmConstants.BlazorPageModuleVersion));
            }
        }        
        activity.SetTag(MasaBlazorWasmConstants.BlazorPagePath, url);
        activity.SetTag(MasaBlazorWasmConstants.HttpRequestTarget, url);
        activity.SetTag(MasaBlazorWasmConstants.HttpRequestUrlFull, NavigationManager.Uri);
        activity.SetTag(MasaBlazorWasmConstants.HttpRequestMethod, "GET");
        SetModule(activity);
        MasaBlazorActivityContent.AddPage(url, page);
        MasaBlazorActivityContent.CurrentActivity = activity;
        MasaBlazorActivityContent.BlazorPage = page;
        Activity.Current = activity;
        return activity;
    }
    #endregion

    #region 小程序采集  
    public static void AfterLocationChangedAsync(string url, object page)
    {
        if (page == null)
        {
            //页面未初始化完成
        }
        var module = RouteUtils.GetModule(url);       

        //没有发生小程序切换
        if (RouteUtils.CurrentModule?.Code == module?.Code)
            return;

        RouteUtils.ChangeModule(module);
    }
    #endregion   
}