using Microsoft.Extensions.Time.Testing;
using Shoplists.Domain.Models.Users;

namespace Shoplists.Application.IntegrationTests.Common;

// Scoped DI carrier for per-test state (current user, time provider). The fixture's CreateScopeFor
// populates it immediately after creating each operation's scope, before any handler resolves
// ICurrentUser or TimeProvider. The values are nullable because they are genuinely unset until that
// priming happens; enforcing "must be set" lives with the consumers (TestCurrentUser throws if the
// user is missing, mirroring the production ICurrentUser; the TimeProvider factory throws likewise).
internal sealed class TestScopeContext
{
    public UserId? UserId { get; private set; }

    public FakeTimeProvider? TimeProvider { get; private set; }

    public void Initialize(UserId userId, FakeTimeProvider timeProvider)
    {
        UserId = userId;
        TimeProvider = timeProvider;
    }
}
