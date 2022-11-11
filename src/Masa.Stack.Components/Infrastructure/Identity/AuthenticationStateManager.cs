namespace Masa.Stack.Components.Infrastructure.Identity;

public class AuthenticationStateManager : IScopedDependency
{
    readonly IHttpContextAccessor _contextAccessor;

    HttpContext? _context;

    public HttpContext Context
    {
        get
        {
            var context = _context ?? _contextAccessor?.HttpContext;
            if (context == null)
            {
                throw new InvalidOperationException("HttpContext must not be null.");
            }
            return context;
        }
        set
        {
            _context = value;
        }
    }


    public AuthenticationStateManager(IHttpContextAccessor contextAccessor)
    {
        if (contextAccessor == null)
        {
            throw new ArgumentNullException(nameof(contextAccessor));
        }
        _contextAccessor = contextAccessor;
    }

    public async Task UpsertClaimAsync(string key, string value)
    {
        var identity = Context.User.Identity as ClaimsIdentity;
        if (identity == null)
            return;

        // check for existing claim and remove it
        var existingClaim = identity.FindFirst(key);
        if (existingClaim != null)
            identity.RemoveClaim(existingClaim);

        // add new claim
        identity.AddClaim(new Claim(key, value));
        var user = new ClaimsPrincipal(identity);
        await RefreshSignInAsync(user);
    }

    public virtual async Task RefreshSignInAsync(ClaimsPrincipal userPrincipal)
    {
        var auth = await Context.AuthenticateAsync(await Context.GetCookieAuthenticationSchemeAsync());

        await SignInWithClaimsAsync(userPrincipal, auth?.Properties);
    }

    public virtual async Task SignInWithClaimsAsync(ClaimsPrincipal userPrincipal, AuthenticationProperties? authenticationProperties)
    {
        await Context.SignInAsync(await Context.GetCookieAuthenticationSchemeAsync(),
            userPrincipal, authenticationProperties ?? new AuthenticationProperties());
    }
}
