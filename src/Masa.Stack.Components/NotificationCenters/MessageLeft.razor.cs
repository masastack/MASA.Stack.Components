// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components.NotificationCenters;

public partial class MessageLeft : MasaComponentBase
{
    [Parameter]
    public EventCallback<Guid?> OnClick { get; set; }

    private List<WebsiteMessageChannelModel> _channels = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadData();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    public async Task LoadData()
    {
        _channels = (await McClient.WebsiteMessageService.GetChannelListAsync());
        StateHasChanged();
    }

    private async Task HandleOnClick(Guid? channelId)
    {
        if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync(channelId);
        }
    }
}
