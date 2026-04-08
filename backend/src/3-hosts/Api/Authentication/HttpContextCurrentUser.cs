using System.Security.Claims;
using Shoplists.Application.Common.Authentication;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Api.Authentication;

// Reads the "sub" (subject) claim from the authenticated user's JWT.
// With MapInboundClaims = false, OIDC claim types are preserved as-is.
// Throws if no authenticated user — all API endpoints require auth,
// so reaching this code without a "sub" claim indicates a configuration error.
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
