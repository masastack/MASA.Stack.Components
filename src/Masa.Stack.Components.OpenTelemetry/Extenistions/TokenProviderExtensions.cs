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
}