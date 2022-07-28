// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components.UserCenters;

public class UploadAvatar : SUploadImage
{
    [Inject]
    public IClient Client { get; set; } = default!;

    [Inject]
    public IOptions<SecurityTokenOptions> SecurityTokenOptions { get; set; } = default!;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        Avatar = true;
        Size = 120;
        return base.SetParametersAsync(parameters);
    }

    public override async Task UploadAsync()
    {
        var option = SecurityTokenOptions.Value;
        var response = Client.GetSecurityToken();
        var stsToken = response.SessionToken;
        var accessId = response.AccessKeyId;
        var accessSecret = response.AccessKeySecret;
        var paramter = new SecurityTokenModel(option.Region, accessId, accessSecret, stsToken, option.Bucket);
        OnInputFileUpload = FileUploadCallBack.CreateCallback("UploadImage", paramter);
        await base.UploadAsync();
    }
}

