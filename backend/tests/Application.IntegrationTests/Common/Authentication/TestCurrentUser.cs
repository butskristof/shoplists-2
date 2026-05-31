using Shoplists.Application.Common.Authentication;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Application.IntegrationTests.Common.Authentication;

internal sealed class TestCurrentUser(TestScopeContext context) : ICurrentUser
{
    public UserId UserId => context.UserId;
}
