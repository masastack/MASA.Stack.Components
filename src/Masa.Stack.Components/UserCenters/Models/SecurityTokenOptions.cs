namespace Masa.Stack.Components.UserCenters.Models;

public class SecurityTokenOptions: ConfigurationApiMasaConfigurationOptions
{
    [JsonIgnore]
    public override string AppId => "public-$Config";

    [JsonIgnore]
    public override string? ObjectName { get; } = "SSO";

    public string Region { get; set; }

    public string AccessKeyId { get; set; }

    public string AccessKeySecret { get; set; }

    public string StsToken { get; set; }

    public string Bucket { get; set; }

    public SecurityTokenOptions(string region, string accessKeyId, string accessKeySecret, string stsToken, string bucket)
    {
        Region = region;
        AccessKeyId = accessKeyId;
        AccessKeySecret = accessKeySecret;
        StsToken = stsToken;
        Bucket = bucket;
    }
}
