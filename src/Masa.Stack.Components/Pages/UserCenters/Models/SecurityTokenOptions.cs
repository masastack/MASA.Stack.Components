namespace Masa.Stack.Components.UserCenters.Models;

public class SecurityTokenOptions: ConfigurationApiMasaConfigurationOptions
{
    [JsonIgnore]
    public override string AppId => "public-$Config";

    [JsonIgnore]
    public override string? ObjectName { get; } = "$public.OSS";

    public string Region { get; set; } = "";

    public string AccessKeyId { get; set; } = "";

    public string AccessKeySecret { get; set; } = "";

    public string StsToken { get; set; } = "";

    public string Bucket { get; set; } = "";
}
