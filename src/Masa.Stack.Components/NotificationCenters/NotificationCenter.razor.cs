// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components.NotificationCenters;

public partial class NotificationCenter : MasaComponentBase
{
    [Parameter]
    public string MessageId { get; set; }

    private NotificationLeft _messageLeftRef = default!;
    private NotificationRight _messageRightRef = default!;
    private bool _detailShow = false;
    private Guid? _channelId;
    private Guid _messageId;

    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(MessageId))
        {
            _detailShow = false;
            _messageId = default(Guid);
        }
        else
        {
            _messageId = Guid.Parse(MessageId);
            _detailShow = true;
        }
    }

    private void HandleListItemClick(Guid messageId)
    {
        _messageId = messageId;
        _detailShow = true;
    }

    private void HandleDetailBack()
    {
        _detailShow = false;
    }

    private async void HandleChannelClick(Guid? channelId)
    {
        _channelId = channelId;
        _detailShow = false;
        await _messageRightRef.RefreshAsync();
    }

    private async Task HandleAllRead()
    {
        await _messageLeftRef.LoadData();
    }
}
