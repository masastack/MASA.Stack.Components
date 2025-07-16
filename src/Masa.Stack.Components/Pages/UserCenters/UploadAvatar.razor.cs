// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public partial class UploadAvatar : SUploadImage
{
    [Parameter]
    public bool RandomName { get; set; }

    [Inject]
    public IDccClient Client { get; set; } = default!;

    [Inject]
    private IMultiEnvironmentUserContext MultiEnvironmentUserContext { get; set; } = null!;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        Avatar = true;
        Size = 120;
        IsOverlay = true;
        OverlayTips = I18n?.T("UploadAvatar") ?? string.Empty;
        return base.SetParametersAsync(parameters);
    }

    protected override async Task OnInputFileChange(InputFileChangeEventArgs e)
    {
        if (e.File.ContentType == "image/gif")
        {
            await PopupService.EnqueueSnackbarAsync(T($"Does not support gif format avatar"), AlertTypes.Error);
            return;
        }
        await base.OnInputFileChange(e);
    }

    public override async Task UploadAsync()
    {
        var response = await Client.OpenApiService.GetOssSecurityTokenAsync();
        OnInputFileUpload = FileUploadCallBack.CreateCallback("UploadImage",
            new
            {
                response.AccessKeyId,
                response.AccessKeySecret,
                response.Region,
                response.Bucket,
                response.StsToken,
                response.CdnHost,
                RandomName
            });
        await base.UploadAsync();
    }
}

