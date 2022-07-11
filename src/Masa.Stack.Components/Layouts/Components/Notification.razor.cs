namespace Masa.Stack.Components.Layouts;

public partial class Notification : MasaComponentBase
{
    [Inject]
    public NoticeState NoticeState { get; set; } = default!;

    [Inject]
    public McApiOptions McApiOptions { get; set; } = default!;

    public HubConnection? HubConnection { get; set; }

    private GetNoticeListModel _queryParam = new();

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
            .WithUrl(NavigationManager.ToAbsoluteUri($"{McApiOptions.McServiceBaseAddress}/signalr-hubs/notifications"))
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