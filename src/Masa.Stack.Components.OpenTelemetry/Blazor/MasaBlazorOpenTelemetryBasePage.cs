namespace Masa.Stack.Components.OpenTelemetry.Blazor;

public partial class MasaBlazorOpenTelemetryBasePage : NextTickComponentBase
{
    static ActivitySource ActivitySource => new(MasaBlazorWasmConstants.MasaBlazorWasmActivitySourceName);

    [Inject] ILoggerFactory LoggerFactory { get; set; } = default!;

    [Inject] IJSRuntime JSRuntime { get; set; } = default!;

    [Inject] TokenProvider TokenProvider { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    protected ILogger Logger => LoggerFactory.CreateLogger(GetType());

    internal Activity? Activity { get; set; } = default;

    private bool isPage = false;

    protected override void OnInitialized()
    {
        if (RouteUtils.IsPage(GetType(), NavigationManager, out var routeTemplate))
        {
            isPage = true;
            Activity = NavControllerHelper.StartPageActivity(ActivitySource!, this, NavigationManager, MasaBlazorActivityContent.UserAgent);
            if (Activity != null)
            {
                Activity.SetTag(MasaBlazorWasmConstants.BlazorPageRouter, routeTemplate);
                Activity.SetTag(MasaBlazorWasmConstants.HttpRequestTarget, routeTemplate);
                Activity.DisplayName = routeTemplate!;
            }
        }
        else
        {
            isPage = false;
        }
        base.OnInitialized();
    }

    protected override async Task OnInitializedAsync()
    {
        if (isPage && Activity != null)
        {
            var token = await TokenProvider.GetAccessTokenAsync();
            var masaToken = string.IsNullOrEmpty(token) ? default : TokenProviderExtensions.GetJwtToken(token);
            if (masaToken != null)
            {
                var userId = TokenProviderExtensions.GetUserId(masaToken);
                var userName = TokenProviderExtensions.GetUserName(masaToken);
                var hash = string.IsNullOrEmpty(token) ? string.Empty : string.Join("", SHA1.HashData(Encoding.UTF8.GetBytes(token)).Select(a => a.ToString("X2"))).ToLower();
                if (!string.IsNullOrEmpty(userId))
                {
                    Activity.SetTag(MasaBlazorWasmConstants.LastLoginUserId, userId);
                }
                Activity.SetTag(MasaBlazorWasmConstants.SessionUserId, userId);
                Activity.SetTag(MasaBlazorWasmConstants.SessionUserName, userName);
                if (!string.IsNullOrEmpty(token))
                {
                    Activity.SetTag(MasaBlazorWasmConstants.BlazorPageSessionId, hash);
                }
                NavControllerHelper.RefreshCurrentUserId(userId, userName, hash);

                var ip = await MasaBlazorActivityContent.GetIpAsync();
                if (!string.IsNullOrEmpty(ip))
                    Activity.SetTag(MasaBlazorWasmConstants.ClientIp, ip);
            }
        }
        await base.OnInitializedAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Activity?.SetTag(MasaBlazorWasmConstants.BlazorPageFirstShowTime, MasaBlazorWasmConstants.UnixTimespan(DateTime.Now));
            Activity?.SetTag(MasaBlazorWasmConstants.HttpRequestStatusCode, "200");

            //通过auth菜单获取页面的名称
            var title = GetPageTitle();
            if (!string.IsNullOrEmpty(title))
                Activity?.SetTag(MasaBlazorWasmConstants.BlazorPageTitle, title);
        }
        base.OnAfterRender(firstRender);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && string.IsNullOrEmpty(MasaBlazorActivityContent.UserAgent))
        {
            MasaBlazorActivityContent.UserAgent = await JSRuntime.InvokeAsync<string>("eval", "navigator.userAgent");
            if (!string.IsNullOrEmpty(MasaBlazorActivityContent.UserAgent) && Activity != null)
            {
                Activity.SetTag(MasaBlazorWasmConstants.HttpRequestUserAgent, MasaBlazorActivityContent.UserAgent);
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    protected virtual string? GetPageTitle()
    {
        return Activity?.GetTagItem(MasaBlazorWasmConstants.BlazorPageRouter)?.ToString();
    }

    private void EndPageActivity()
    {
        if (isPage)
        {
            BeforeEnd();
            var url = Activity?.GetTagItem(MasaBlazorWasmConstants.BlazorPagePath)?.ToString();
            if (!string.IsNullOrEmpty(Activity?.Id))
                Activity?.Stop();
            if (!string.IsNullOrEmpty(url))
            {
                MasaBlazorActivityContent.RemovePage(url);
            }
        }
    }

    private void BeforeEnd()
    {
        var toPath = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/");
        var page = MasaBlazorActivityContent.GetPage(MasaBlazorActivityContent.CurrentUrl!);
        if (page == null)
            return;
        if (page == this)
            return;
        var activity = ((MasaBlazorOpenTelemetryBasePage)page).Activity;
        if (activity == null || string.IsNullOrEmpty(activity.Id))
            return;
        var fromUrl = Activity?.GetTagItem(MasaBlazorWasmConstants.BlazorPagePath)?.ToString();
        if (!string.IsNullOrEmpty(fromUrl) && fromUrl != toPath)
        {
            activity?.SetTag(MasaBlazorWasmConstants.BlazorPageFromPath, Activity?.GetTagItem(MasaBlazorWasmConstants.BlazorPagePath));
            activity?.SetTag(MasaBlazorWasmConstants.BlazorPageFromTitle, Activity?.GetTagItem(MasaBlazorWasmConstants.BlazorPageTitle));
            Activity?.SetTag(MasaBlazorWasmConstants.BlazorPageToPath, toPath);
        }
    }

    protected override ValueTask DisposeAsyncCore()
    {
        EndPageActivity();
        return base.DisposeAsyncCore();
    }

    public IDisposable? GetLogScope(ILogger? logger = null)
    {
        return logger?.BeginScope(GetLogScopeValues());
    }

    public IDictionary<string, object> GetLogScopeValues()
    {
        var currentAcitivity = MasaBlazorActivityContent.CurrentActivity;
        if (currentAcitivity == null)
            return new Dictionary<string, object>();
        var dicSope = new Dictionary<string, object>();
        var lastUserId = currentAcitivity.GetTagItem(MasaBlazorWasmConstants.LastLoginUserId)?.ToString();
        if (!string.IsNullOrEmpty(lastUserId))
            dicSope.Add(MasaBlazorWasmConstants.LastLoginUserId, lastUserId);

        string[] items = [
            MasaBlazorWasmConstants.BlazorPagePath,
            MasaBlazorWasmConstants.BlazorPageRouter,
            MasaBlazorWasmConstants.BlazorPageModuleCode,
            MasaBlazorWasmConstants.BlazorPageModuleName,
            MasaBlazorWasmConstants.BlazorPageModuleVersion,
            MasaBlazorWasmConstants.ClientIp
            ];
        foreach (var key in items)
        {
            var value = currentAcitivity.GetTagItem(key)?.ToString();
            if (string.IsNullOrEmpty(value)) continue;
            dicSope.Add(key, value);
        }
        dicSope.Add("traceid", currentAcitivity.TraceId);
        dicSope.Add("spanid", currentAcitivity.SpanId);
        return dicSope;
    }
}
