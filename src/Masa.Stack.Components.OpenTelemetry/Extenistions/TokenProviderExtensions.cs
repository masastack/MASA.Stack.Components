namespace Masa.Stack.Components.OpenTelemetry.Extenistions;

internal static class TokenProviderExtensions
{
    private static readonly JwtSecurityTokenHandler jwtSecurityTokenHandler = new();

    public static JwtSecurityToken? GetJwtToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return default;
        try
        {
            return jwtSecurityTokenHandler.ReadJwtToken(token);
        }
        catch
        {
            return default;
        }
    }

    public static string? GetUserId(JwtSecurityToken? token) => token?.Claims.FirstOrDefault(claim => claim.Type == IdentityClaimConsts.USER_ID)?.Value;

    public static string? GetUserName(JwtSecurityToken? token) => token?.Claims.FirstOrDefault(claim => claim.Type == IdentityClaimConsts.USER_NAME)?.Value;

    public static string? GetPhone(JwtSecurityToken? token) => token?.Claims.FirstOrDefault(claim => claim.Type == IdentityClaimConsts.PHONE_NUMBER)?.Value;

    /// <summary>
    /// 取模拟登录操作者的 id（RFC 8693 <c>act.sub</c>）。非模拟 token（无 act / act 畸形）返回 null。
    /// 遥测据此把「代操作」归因到真实的操作者，而不是只归因到被模拟用户。
    /// </summary>
    public static string? GetActorId(JwtSecurityToken? token) => GetActorProperty(token, "sub");

    /// <summary>取模拟登录操作者的显示名（<c>act.userName</c> 附加属性，发送端可选提供）。</summary>
    public static string? GetActorName(JwtSecurityToken? token) => GetActorProperty(token, "userName");

    /// <summary>读取 act claim（JSON 对象）里的字符串属性；act 缺失或畸形时返回 null，绝不抛异常。</summary>
    private static string? GetActorProperty(JwtSecurityToken? token, string propertyName)
    {
        var act = token?.Claims.FirstOrDefault(claim => claim.Type == "act")?.Value;
        if (string.IsNullOrEmpty(act))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(act);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty(propertyName, out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }
        catch (JsonException)
        {
        }
        return null;
    }
}