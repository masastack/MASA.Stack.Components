// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components.Extensions;

public static class OssExtensions
{
    public static void AddOss(this IServiceCollection services)
    {
        var ossOptions = services.BuildServiceProvider()
                         .GetRequiredService<IMasaConfiguration>()
                         .ConfigurationApi
                         .GetPublic()
                         .GetSection("$public.OSS")
                         .Get<OssOptions>();
        services.AddAliyunStorage(new AliyunStorageOptions(ossOptions.AccessId, ossOptions.AccessSecret, ossOptions.Endpoint, ossOptions.RoleArn, ossOptions.RoleSessionName)
        {
            Sts = new AliyunStsOptions()
            {
                RegionId = ossOptions.RegionId
            }
        });
    }
}