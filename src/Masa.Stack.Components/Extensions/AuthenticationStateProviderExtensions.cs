namespace Masa.Stack.Components.Extensions;

public static class AuthenticationStateProviderExtensions
{
    public static async Task UpsertClaimAsync(this AuthenticationStateProvider authenticationStateProvider, string key, string value)
    {
        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var identity = authenticationState.User.Identity as ClaimsIdentity;
        if (identity == null)
            return;

        // check for existing claim and remove it
        var existingClaim = identity.FindFirst(key);
        if (existingClaim != null)
            identity.RemoveClaim(existingClaim);

        // add new claim
        identity.AddClaim(new Claim(key, value));
        var user = new ClaimsPrincipal(identity);
        authenticationState = new AuthenticationState(user);
        if (authenticationStateProvider is IHostEnvironmentAuthenticationStateProvider serverAuthenticationStateProvider)
        {
            serverAuthenticationStateProvider.SetAuthenticationState(Task.FromResult(authenticationState));
        }
    }

    public static async Task<string?> GetClaimValueAsync(this AuthenticationStateProvider authenticationStateProvider, string key)
    {
        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var identity = authenticationState.User.Identity as ClaimsIdentity;
        if (identity == null)
            return null;

        var claim = identity.Claims.FirstOrDefault(c => c.Type == key);

        return claim?.Value;
    }
}
