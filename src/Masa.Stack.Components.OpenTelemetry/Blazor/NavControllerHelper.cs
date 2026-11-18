namespace Masa.Stack.Components.OpenTelemetry.Blazor;

internal static class NavControllerHelper
{
    private static string? _currentUserId;
    private static string? _currentUserName;
    private static string? _currentTokenHash;
    private static string? _currentActorUserId;
    private static string? _currentActorUserName;

    public static void RefreshCurrentUserId(string? userId, string? name, string? tokenHash,
        string? actorId = null, string? actorName = null)
    {
        _currentUserId = userId;
        _currentUserName = name;
        _currentTokenHash = tokenHash;
        _currentActorUserId = actorId;
        _currentActorUserName = actorName;
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
        // 模拟登录的操作者也要随静态快照传播到后续页面活动，否则只有首个活动带审计归因
        if (!string.IsNullOrEmpty(_currentActorUserId))
            activity.SetTag(MasaBlazorWasmConstants.SessionActorUserId, _currentActorUserId);
        if (!string.IsNullOrEmpty(_currentActorUserName))
            activity.SetTag(MasaBlazorWasmConstants.SessionActorUserName, _currentActorUserName);

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
        // 不要直接用 NavigationManager.Uri：它可能带着 #access_token=...（模拟登录入口），见 RouteUtils.RemoveFragment。
        var currentUri = RouteUtils.RemoveFragment(NavigationManager.Uri);
        var url = currentUri.Replace(NavigationManager.BaseUri, prefix);
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
        activity.SetTag(MasaBlazorWasmConstants.HttpRequestUrlFull, currentUri);
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

    #region 用户身份与模拟审计归因

    /// <summary>
    /// 从访问令牌解析用户身份并写入活动标签（enduser.id / nick_name / session_id），返回是否成功。
    /// <para>
    /// 模拟登录（RFC 8693 token-exchange）期间，<paramref name="token"/> 是被模拟用户的短命 token，
    /// 因此 <c>enduser.*</c> 会如实显示被模拟用户——这是特性而非缺陷（页面活动确实以被模拟者身份发生）。
    /// 但 token 里的 <c>act</c> claim 记录了真正的操作者，必须一并写入 <c>enduser.actor.*</c>，
    /// 否则模拟窗口内的所有页面活动在 OTLP 后端都被归因到被模拟用户，无法区分「代操作」，审计就断了。
    /// </para>
    /// <para>token 为空或无法解析时不动任何标签，返回 false（与页面原先的 <c>hasToken &amp;&amp; masaToken != null</c> 语义一致）。</para>
    /// </summary>
    public static bool SetUserIdentity(Activity? activity, string? token)
    {
        if (activity == null)
        {
            return false;
        }

        var masaToken = TokenProviderExtensions.GetJwtToken(token);
        if (masaToken == null)
        {
            return false;
        }

        var userId = TokenProviderExtensions.GetUserId(masaToken);
        var userName = TokenProviderExtensions.GetUserName(masaToken);
        var actorId = TokenProviderExtensions.GetActorId(masaToken);
        var actorName = TokenProviderExtensions.GetActorName(masaToken);
        var hash = string.Join("", SHA1.HashData(Encoding.UTF8.GetBytes(token!)).Select(a => a.ToString("X2"))).ToLower();

        if (!string.IsNullOrEmpty(userId))
            activity.SetTag(MasaBlazorWasmConstants.LastLoginUserId, userId);
        activity.SetTag(MasaBlazorWasmConstants.SessionUserId, userId);
        activity.SetTag(MasaBlazorWasmConstants.SessionUserName, userName);
        activity.SetTag(MasaBlazorWasmConstants.BlazorPageSessionId, hash);

        if (!string.IsNullOrEmpty(actorId))
        {
            activity.SetTag(MasaBlazorWasmConstants.SessionActorUserId, actorId);
            if (!string.IsNullOrEmpty(actorName))
            {
                activity.SetTag(MasaBlazorWasmConstants.SessionActorUserName, actorName);
            }
        }

        // 同步静态快照：后续 StartPageActivity 的 SetModule 会把同一组身份（含操作者）打到新活动上
        RefreshCurrentUserId(userId, userName, hash, actorId, actorName);
        return true;
    }
    #endregion
}