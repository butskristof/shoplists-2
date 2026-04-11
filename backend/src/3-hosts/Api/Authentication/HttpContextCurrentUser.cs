using System.Security.Claims;
using Shoplists.Application.Common.Authentication;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Api.Authentication;

/// <summary>
/// Provides access to the current authenticated user's identifier by reading the "sub" (subject) claim from the JSON
/// Web Token (JWT) of the authenticated user. Ensures that the claim is present and valid for all authenticated API
/// requests.
/// </summary>
/// <remarks>
/// This class relies on the <see cref="IHttpContextAccessor"/> to access the current HTTP context. The "sub" claim must
/// be included in the JWT token issued by the authentication provider. If no authenticated user or "sub" claim is
/// found, an exception is thrown, as reaching this code without proper authentication indicates a configuration error.
/// </remarks>
/// <exception cref="InvalidOperationException">
/// Thrown if the "sub" claim is missing from the authenticated user's JWT token. This likely indicates that the JWT
/// Bearer authentication is not configured correctly or claims mapping settings are incorrect.
/// </exception>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    private Claim? FindFirstClaim(string type) => User?.FindFirst(type);

    private string? FindFirstClaimValue(string type) => FindFirstClaim(type)?.Value;

    public UserId UserId
    {
        get
        {
            var sub = FindFirstClaimValue("sub");

            if (string.IsNullOrEmpty(sub))
                throw new InvalidOperationException(
                    "No 'sub' claim found on the authenticated user. Ensure JWT Bearer authentication is configured correctly."
                );

            return new UserId(sub);
        }
    }
}
