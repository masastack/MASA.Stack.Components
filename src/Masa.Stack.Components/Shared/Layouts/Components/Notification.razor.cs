using Microsoft.AspNetCore.Authentication;

namespace Masa.Stack.Components.Layouts;

public partial class Notification : MasaComponentBase
{
    [Inject]
    public NoticeState NoticeState { get; set; } = default!;

    [Inject]
    public McServiceOptions McApiOptions { get; set; } = default!;

    public HubConnection? HubConnection { get; set; }

    private GetNoticeListModel _queryParam = new();
    private bool _showMenu;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        NoticeState.OnNoticeChanged += Changed;

        await McClient.WebsiteMessageService.CheckAsync();

        await HubConnectionBuilder();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadData();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task HubConnectionBuilder()
    {
        HubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri($"{McApiOptions.BaseAddress}/signalr-hubs/notifications"), options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    string? accessToken = string.Empty;
                    if (httpContextAccessor.HttpContext != null)
                    {
                        accessToken = await httpContextAccessor.HttpContext.GetTokenAsync("access_token");
                    }
                    return accessToken;
                };
            })
            .Build();
        await HubConnection.StartAsync();

        HubConnection?.On(SignalRMethodConsts.GET_NOTIFICATION, async () =>
        {
            await LoadData();
        });

        HubConnection?.On(SignalRMethodConsts.CHECK_NOTIFICATION, async () =>
        {
            await McClient.WebsiteMessageService.CheckAsync();
        });
    }

    private void HandleOpenOnClick()
    {
        if (NoticeState.Notices.Any())
        {
            _showMenu = true;
        }
        else
        {
            _showMenu = false;
            NavigationManager.NavigateTo("/notification-center", true);
        }
    }

    async Task LoadData()
    {
        var dtos = await McClient.WebsiteMessageService.GetNoticeListAsync(_queryParam);
        NoticeState.SetNotices(dtos);
    }

    async Task Changed()
    {
        await InvokeAsync(StateHasChanged);
    }

    public async void Dispose()
    {
        NoticeState.OnNoticeChanged -= Changed;
        await HubConnection.DisposeAsync();
    }
}