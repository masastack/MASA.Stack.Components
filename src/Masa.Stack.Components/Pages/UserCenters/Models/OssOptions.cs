namespace Masa.Stack.Components.UserCenters.Models;

public class OssOptions: ConfigurationApiMasaConfigurationOptions
{
    [JsonIgnore]
    public override string AppId => "public-$Config";

    [JsonIgnore]
    public override string? ObjectName { get; } = "$public.OSS";

    public string AccessId { get; set; } = "";

    public string AccessSecret { get; set; } = "";

    public string Bucket { get; set; } = "";

    public string Endpoint { get; set; } = "";

    public string RoleArn { get; set; } = "";

    public string RoleSessionName { get; set; } = "";

    public string RegionId { get; set; } = "";
}
