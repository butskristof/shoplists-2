using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Shoplists.Application.Common.Authentication;
using Shoplists.Application.IntegrationTests.Common.Authentication;
using Shoplists.Domain.Models.Users;
using Shoplists.Persistence;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Shoplists.Application.IntegrationTests.Common;

// Session-scoped fixture: owns the Postgres container, the host, and the DI graph. Per-test state
// (current user, FakeTimeProvider) lives on IntegrationTestBase — TUnit creates a fresh test class
// instance per [Test] method, so those fields are naturally isolated per test. CreateScopeFor
// produces a primed DI scope that resolves ICurrentUser and TimeProvider to the test's values
// (via the internal TestScopeContext); callers don't need to know that mechanism exists.
public sealed class ApplicationFixture : IAsyncInitializer, IAsyncDisposable
{
    // Keep the image tag in sync with AppHost.cs (`.WithImageTag("17.6")`). Both sides pin
    // explicitly so production and tests exercise the same Postgres engine version.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(
        "postgres:17.6"
    ).Build();

    private IServiceScopeFactory _scopeFactory = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddApplication();
        builder.AddPersistence(connectionString: _container.GetConnectionString());
        builder.Services.AddScoped<TestScopeContext>();
        builder.Services.AddScoped<ICurrentUser, TestCurrentUser>();
        builder.Services.AddScoped<TimeProvider>(sp =>
            sp.GetRequiredService<TestScopeContext>().TimeProvider
            // should be sent through IntegrationTestBase.SendAsync
            ?? throw new InvalidOperationException("No time provider set on the test scope.")
        );

        var host = builder.Build();
        await DatabaseMigrationRunner.RunMigrationsAsync(host.Services, CancellationToken.None);
        _scopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();
    }

    internal IServiceScope CreateScopeFor(UserId userId, FakeTimeProvider timeProvider)
    {
        var scope = _scopeFactory.CreateScope();
        scope
            .ServiceProvider.GetRequiredService<TestScopeContext>()
            .Initialize(userId, timeProvider);
        return scope;
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
