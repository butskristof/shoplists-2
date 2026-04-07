using System.Diagnostics;
using Shoplists.Persistence;

namespace Shoplists.DatabaseMigrator;

/// <summary>
/// Applies pending EF Core database migrations, then signals the host to shut down.
///
/// This runs as a <see cref="BackgroundService"/> inside a Worker Service host that is
/// orchestrated by Aspire. The lifecycle is:
///
/// 1. Aspire starts the Postgres container and waits for it to be healthy.
/// 2. Aspire starts this DatabaseMigrator project (WaitFor postgres).
/// 3. This worker applies migrations via <see cref="DatabaseMigrationRunner"/>.
/// 4. On success, <see cref="IHostApplicationLifetime.StopApplication"/> signals the host to exit
///    with code 0 (success).
/// 5. Aspire sees the process exit and unblocks dependent resources (API) that use
///    WaitForCompletion on this project.
///
/// On failure, the exception propagates, the host exits with a non-zero code, and Aspire
/// marks this resource as failed. Dependent resources remain blocked and never start,
/// making the failure immediately visible in the Aspire dashboard.
/// </summary>
internal sealed class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<Worker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sw = Stopwatch.StartNew();

        logger.LogInformation("Applying database migrations");
        await DatabaseMigrationRunner
            .RunMigrationsAsync(serviceProvider, stoppingToken)
            .ConfigureAwait(false);
        logger.LogInformation(
            "Database migrations completed in {ElapsedMilliseconds}ms",
            sw.ElapsedMilliseconds
        );

        // Signal the generic host to shut down gracefully. This causes the process to exit
        // with code 0, which Aspire interprets as successful completion.
        hostApplicationLifetime.StopApplication();
    }
}
