﻿// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components;

public class UploadAvatar : SUploadImage
{
    [Inject]
    public IClient Client { get; set; } = default!;

    [Inject]
    public IOptions<OssOptions> OssOptions { get; set; } = default!;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        Avatar = true;
        Size = 120;
        return base.SetParametersAsync(parameters);
    }

    public override async Task UploadAsync()
    {
        var response = Client.GetSecurityToken();
        var stsToken = response.SessionToken;
        var accessId = response.AccessKeyId;
        var accessSecret = response.AccessKeySecret;
        var region = "oss-cn-hangzhou";
        var bucket = OssOptions.Value.Bucket;

        var paramter = new SecurityTokenModel(region, accessId, accessSecret, stsToken, bucket);
        OnInputFileUpload = FileUploadCallBack.CreateCallback("UploadImage", paramter);
        await base.UploadAsync();
    }
}
