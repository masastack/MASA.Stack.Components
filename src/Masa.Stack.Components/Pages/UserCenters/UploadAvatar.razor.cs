// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public partial class UploadAvatar : SUploadImage
{
    [Inject]
    public IObjectStorageClient Client { get; set; } = default!;

    [Inject]
    public IConfiguration Configuration { get; set; } = default!;
    
    public OssOptions OssOptions
    {
        get
        {
            return Configuration.GetSection("ConfigurationApi:public-$Config:$public.oss").Get<OssOptions>();
        }
    }

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
        if(e.File.ContentType == "image/gif")
        {
            await PopupService.EnqueueSnackbarAsync(T($"Does not support gif format avatar"), AlertTypes.Error);
            return;
        }
        await base.OnInputFileChange(e);
    }

    public override async Task UploadAsync()
    {
        var response = Client.GetSecurityToken();
        var stsToken = response.SessionToken;
        var accessId = response.AccessKeyId;
        var accessSecret = response.AccessKeySecret;
        var region = "oss-cn-hangzhou";
        var bucket = OssOptions.Bucket;

        var paramter = new SecurityTokenModel(region, accessId, accessSecret, stsToken, bucket);
        OnInputFileUpload = FileUploadCallBack.CreateCallback("UploadImage", paramter);
        await base.UploadAsync();
    }
}

