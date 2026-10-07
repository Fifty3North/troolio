using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Sample.Api;

public sealed record DemoUser(string Name, Guid Id, string Token);
public static class DemoUsers
{
    public static readonly DemoUser[] All =
    [
        new("Alice", Guid.Parse("11111111-1111-1111-1111-111111111111"), "demo-alice"),
        new("Bob", Guid.Parse("22222222-2222-2222-2222-222222222222"), "demo-bob"),
        new("Carol", Guid.Parse("33333333-3333-3333-3333-333333333333"), "demo-carol")
    ];
}

// Local teaching identities, deliberately disabled outside Development/Testing.
public sealed class DemoAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers.Authorization.ToString();
        var user = DemoUsers.All.SingleOrDefault(x => token == $"Bearer {x.Token}");
        if (user is null) return Task.FromResult(AuthenticateResult.NoResult());
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Name)], Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
