using Microsoft.Extensions.Time.Testing;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Application.IntegrationTests.Common;

// Scoped DI service that carries per-test state (current user, time provider) into each operation's
// DI scope. IntegrationTestBase.SendAsync populates this immediately after creating the scope, so
// the test's ICurrentUser and TimeProvider resolutions inside the handler see the test's state.
internal sealed class TestScopeContext
{
    public UserId UserId { get; set; }

    public FakeTimeProvider TimeProvider { get; set; } = null!;
}
