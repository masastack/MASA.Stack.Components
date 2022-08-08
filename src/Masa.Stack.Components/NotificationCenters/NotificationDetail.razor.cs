// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components.NotificationCenters;

public partial class NotificationDetail
{
    [Inject]
    public NoticeState NoticeState { get; set; } = default!;

    [Inject]
    private IPopupService PopupService { get; set; } = null!;

    [Parameter]
    public Guid MessageId { get; set; }

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback OnBack { get; set; }

    [Parameter]
    public EventCallback<Guid?> OnChannelClick { get; set; }

    private WebsiteMessageModel _entity = new();

    protected override async void OnParametersSet()
    {
        if (Visible)
        {
            await GetFormDataAsync(MessageId);
        }
    }

    private async Task GetFormDataAsync(Guid id)
    {
        _entity = await McClient.WebsiteMessageService.GetAsync(id) ?? new();
        if (!_entity.IsRead)
        {
            await McClient.WebsiteMessageService.ReadAsync(new ReadWebsiteMessageModel { Id = id });
        }
        StateHasChanged();
    }

    private async Task HandleDelAsync()
    {
        if (await PopupService.ConfirmAsync(T("OperationConfirmation"), T("DeletionConfirmationMessage"), AlertTypes.Warning)) await DeleteAsync();
    }

    private async Task DeleteAsync()
    {
        await McClient.WebsiteMessageService.DeleteAsync(MessageId);
        await PopupService.ToastSuccessAsync(T("DeletedSuccessfullyMessage"));

        await LoadNotices();

        await HandleOnBack();
    }

    private async Task LoadNotices()
    {
        GetNoticeListModel _queryParam = new();
        var dtos = await McClient.WebsiteMessageService.GetNoticeListAsync(_queryParam);
        NoticeState.SetNotices(dtos);
    }

    private async Task HandleOnBack()
    {
        if (OnBack.HasDelegate)
        {
            await OnBack.InvokeAsync();
        }
    }

    private async Task HandlePrevAndNext(Guid id)
    {
        if (id == default) return;
        await GetFormDataAsync(id);
    }

    private async Task HandleOnChannelClick()
    {
        if (OnChannelClick.HasDelegate)
        {
            await OnChannelClick.InvokeAsync(_entity?.ChannelId);
        }
    }
}
