using Masa.BuildingBlocks.BasicAbility.Mc.Model;
using Masa.Contrib.BasicAbility.Mc.Service;
using Masa.Stack.Components.Store;

namespace Masa.Stack.Components.Layouts;

public partial class Notification : MasaComponentBase
{
    [Inject]
    public NoticeState NoticeState { get; set; } = default!;

    private GetNoticeListModel _queryParam = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        NoticeState.OnNoticeChanged += Changed;

        await McClient.WebsiteMessageService.CheckAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadData();
        }

        if (!NoticeState.IsHubConnectionBuilder)
        {
            NoticeState.IsHubConnectionBuilder = true;
            //await base.HubConnectionBuilder();

            //base.HubConnection?.On(SignalRMethodConsts.GET_NOTIFICATION, async () =>
            //{
            //    await LoadData();
            //});

            //base.HubConnection?.On(SignalRMethodConsts.CHECK_NOTIFICATION, async () =>
            //{
            //    await WebsiteMessageService.CheckAsync();
            //});
        }
        await base.OnAfterRenderAsync(firstRender);
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

    public void Dispose()
    {
        NoticeState.OnNoticeChanged -= Changed;
    }
}