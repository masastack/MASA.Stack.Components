﻿using Masa.Stack.Components.Rcl.Shared.IntegrationComponents.Confirm;

namespace Masa.Stack.Components.Rcl.Extensions;

public static class PopupServiceExtensions
{
    public static async Task<bool> SimpleConfirmAsync(this IPopupService popupService, string content)
    {
        return await popupService.SimpleConfirmAsync(null, content, AlertTypes.None);
    }

    public static async Task<bool> SimpleConfirmAsync(this IPopupService popupService, string? title, string content)
    {
        return await popupService.SimpleConfirmAsync(title, content, AlertTypes.None);
    }

    public static async Task<bool> SimpleConfirmAsync(this IPopupService popupService, string? title, string content, AlertTypes type)
    {
        var confirm = await popupService.OpenAsync(typeof(SSimpleConfirm), new Dictionary<string, object>()
            {
                { nameof(SSimpleConfirm.Type), type },
                { nameof(SSimpleConfirm.Title), title },
                { nameof(SSimpleConfirm.Content), content },
                { nameof(SSimpleConfirm.ContentClass),"text-break" }
            });

        if (confirm is bool value)
        {
            return value;
        }

        return false;
    }
}
