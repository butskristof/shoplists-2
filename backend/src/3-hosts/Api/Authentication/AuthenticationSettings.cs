namespace Shoplists.Api.Authentication;

internal sealed record AuthenticationSettings
{
    public const string SectionName = "Authentication";

    public required string Authority { get; init; }
    public required string Audience { get; init; }
}
