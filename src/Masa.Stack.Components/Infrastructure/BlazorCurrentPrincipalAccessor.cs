namespace Masa.Stack.Components.Infrastructure;

internal class BlazorCurrentPrincipalAccessor : ICurrentPrincipalAccessor
{
    readonly AuthenticationStateProvider _authenticationStateProvider;

    public BlazorCurrentPrincipalAccessor(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    public ClaimsPrincipal? GetCurrentPrincipal()
    {
        return _authenticationStateProvider.GetAuthenticationStateAsync().Result.User;
    }
}
