using Microsoft.AspNetCore.Authentication;

namespace CareLink.Api.Security;

public sealed class StaticTokenAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string Scheme = "StaticToken";

    public List<StaticTokenDefinition> Tokens { get; set; } = [];
}

public sealed class StaticTokenDefinition
{
    public required string Token { get; init; }
    public required string Subject { get; init; }
    public required string DisplayName { get; init; }
    public required string Role { get; init; }
    public string? FacilityId { get; init; }
}
