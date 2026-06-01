using Shoplists.Domain.Models.Users;

namespace Shoplists.Testing.Common.TestData;

// Test-only affordance: mint a unique UserId. In production a UserId is the OIDC subject and is
// never self-generated, so this lives in test infrastructure (visible only where this namespace is
// imported) rather than on the domain type.
public static class UserIdTestExtensions
{
    extension(UserId)
    {
        public static UserId New() => new(Guid.NewGuid().ToString());
    }
}
