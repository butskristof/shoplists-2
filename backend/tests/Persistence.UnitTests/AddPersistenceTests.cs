using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shoplists.Application.Common.Persistence;

namespace Shoplists.Persistence.UnitTests;

public sealed class AddPersistenceTests
{
    private const string ConnectionString =
        "Host=localhost;Database=shoplists;Username=postgres;Password=postgres";

    [Test]
    public async Task ValidConnectionString_RegistersDbContext()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddPersistence(connectionString: ConnectionString);

        // Reaching registration (rather than throwing) confirms the happy path past the
        // connection-string guard
        await Assert
            .That(builder.Services.Any(d => d.ServiceType == typeof(IAppDbContext)))
            .IsTrue();
    }

    [Test]
    public async Task ConnectionNameInConfiguration_RegistersDbContext()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:db"] = ConnectionString,
            }
        );

        builder.AddPersistence(connectionName: "db");

        // The connection string is resolved from configuration by name, then registration proceeds.
        await Assert
            .That(builder.Services.Any(d => d.ServiceType == typeof(IAppDbContext)))
            .IsTrue();
    }

    [Test]
    public async Task BothConnectionNameAndStringProvided_ThrowsArgumentException()
    {
        var builder = Host.CreateApplicationBuilder();

        await Assert
            .That(() =>
                builder.AddPersistence(connectionName: "db", connectionString: "Host=localhost")
            )
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task NeitherConnectionNameNorStringProvided_ThrowsArgumentException()
    {
        var builder = Host.CreateApplicationBuilder();

        await Assert.That(() => builder.AddPersistence()).Throws<ArgumentException>();
    }

    [Test]
    public async Task ConnectionNameNotInConfiguration_ThrowsInvalidOperationException()
    {
        var builder = Host.CreateApplicationBuilder();

        await Assert
            .That(() => builder.AddPersistence(connectionName: "does-not-exist"))
            .Throws<InvalidOperationException>();
    }
}
