using Shoplists.Application.Common.Authentication;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Application.IntegrationTests.Common.Authentication;

internal sealed class TestCurrentUser(TestScopeContext context) : ICurrentUser
{
    // Mirrors the production ICurrentUser (HttpContextCurrentUser): the implementation is
    // responsible for failing when no user is in context
    public UserId UserId =>
        context.UserId
        // should be sent through IntegrationTestBase.SendAsync which sets current user
        ?? throw new InvalidOperationException("No current user set on the test scope.");
}
