using Microsoft.Extensions.Hosting;

namespace Shoplists.Persistence.UnitTests;

public sealed class AddPersistenceTests
{
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
