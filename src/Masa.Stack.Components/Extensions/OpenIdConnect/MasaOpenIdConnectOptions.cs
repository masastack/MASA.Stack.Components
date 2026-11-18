namespace Masa.Stack.Components.Extensions.OpenIdConnect;

public class MasaOpenIdConnectOptions
{
    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// 模拟登录 token 的预期签发者（JWT <c>iss</c>）。留空则使用 <see cref="Authority"/>（忽略尾部 '/' 与大小写）。
    /// 仅当 SSO 实际签发的 <c>iss</c> 与应用访问 SSO 用的 Authority 不同
    /// （例如经内网网关访问、或 SSO 显式配置了 IssuerUri）时才需要设置，否则模拟 token 会被接收端拒绝。
    /// </summary>
    public string? Issuer { get; set; }

    public List<string> Scopes { get; set; } = new List<string>();
}