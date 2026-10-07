using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace CareLink.Api.Security;

public sealed class StaticTokenAuthenticationHandler(
    IOptionsMonitor<StaticTokenAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<StaticTokenAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var headerValue))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authorization = headerValue.ToString();
        const string prefix = "Bearer ";
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                AuthenticateResult.Fail("Authorization must use the Bearer scheme."));
        }

        var suppliedToken = authorization[prefix.Length..].Trim();
        var definition = Options.Tokens.FirstOrDefault(candidate =>
            string.Equals(candidate.Token, suppliedToken, StringComparison.Ordinal));

        if (definition is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("The bearer token is invalid."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, definition.Subject),
            new(ClaimTypes.Name, definition.DisplayName),
            new(ClaimTypes.Role, definition.Role)
        };

        if (!string.IsNullOrWhiteSpace(definition.FacilityId))
        {
            claims.Add(new Claim("facility_id", definition.FacilityId));
        }

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, StaticTokenAuthenticationOptions.Scheme));
        var ticket = new AuthenticationTicket(
            principal,
            StaticTokenAuthenticationOptions.Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
