namespace Masa.Stack.Components.Rcl.Pages.UserCenters.Models;

public class SecurityTokenModel
{
    public string Region { get; set; }

    public string AccessKeyId { get; set; }

    public string AccessKeySecret { get; set; }

    public string StsToken { get; set; }

    public string Bucket { get; set; }

    public SecurityTokenModel(string region, string accessKeyId, string accessKeySecret, string stsToken, string bucket)
    {
        Region = region;
        AccessKeyId = accessKeyId;
        AccessKeySecret = accessKeySecret;
        StsToken = stsToken;
        Bucket = bucket;
    }
}
