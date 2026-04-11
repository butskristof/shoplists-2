namespace Shoplists.Api.Authentication;

internal sealed record AuthenticationSettings
{
    public const string SectionName = "Authentication";

    public required string Authority { get; init; }
    public required string Audience { get; init; }

    // Used by the OpenAPI OAuth2 security scheme and Scalar UI (dev-only)
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? AuthorizationUrl { get; init; }
    public string? TokenUrl { get; init; }
}
