namespace Masa.Stack.Components.Infrastructure;

public class DefaultServerAuthenticationStateProvider : ServerAuthenticationStateProvider
{
    private Task<AuthenticationState>? _authenticationStateTask;
    private ClaimsPrincipal ClaimsPrincipal { get; set; }

    public DefaultServerAuthenticationStateProvider(IHttpContextAccessor contextAccessor)
    {
        ClaimsPrincipal = contextAccessor.HttpContext?.User ?? new();
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return _authenticationStateTask ?? Task.FromResult(new AuthenticationState(ClaimsPrincipal));
    }
}
