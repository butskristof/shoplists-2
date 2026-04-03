using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shoplists.ServiceDefaults.Constants;

namespace Shoplists.ServiceDefaults;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddServiceDefaults()
        {
            builder.ConfigureOpenTelemetry();

            builder.AddDefaultHealthChecks();

            builder.Services.AddServiceDiscovery();

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // Turn on resilience by default
                http.AddStandardResilienceHandler();

                // Turn on service discovery by default
                http.AddServiceDiscovery();
            });

            return builder;
        }

        private TBuilder ConfigureOpenTelemetry()
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder
                .Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();
                })
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder
                        .AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation(traceInstrumentationOptions =>
                            // Exclude health check requests from tracing
                            traceInstrumentationOptions.Filter = context =>
                                !context.Request.Path.StartsWithSegments(
                                    HealthCheckConstants.BasePath,
                                    StringComparison.OrdinalIgnoreCase
                                )
                        )
                        .AddHttpClientInstrumentation();
                });

            builder.AddOpenTelemetryExporters();

            return builder;
        }

        private TBuilder AddOpenTelemetryExporters()
        {
            var useOtlpExporter = builder.Configuration.HasValue(
                ConfigurationConstants.OtelExporterOtlpEndpoint
            );

            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            return builder;
        }

        private TBuilder AddDefaultHealthChecks()
        {
            builder
                .Services.AddHealthChecks()
                // Trivial liveness check: always returns Healthy.
                // Tagged "live" so the /health/live endpoint picks it up.
                // This check exists solely to verify the process can respond to HTTP requests.
                .AddCheck(
                    HealthCheckConstants.Names.Self,
                    () => HealthCheckResult.Healthy(),
                    [HealthCheckConstants.Tags.Live]
                );

            // Integration health checks (e.g. Postgres, Redis) are registered without the "live" tag
            // by Aspire integrations, so they only participate in the readiness endpoint.

            return builder;
        }
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // Readiness: runs ALL registered health checks (self + dependencies like DB, cache, ...).
            // A failure means "don't send me traffic" — the service can't do useful work right now.
            app.MapHealthChecks(HealthCheckConstants.Endpoints.Ready);

            // Liveness: runs ONLY checks tagged "live" (currently just the trivial "self" check).
            // A failure means "the process is stuck/deadlocked, restart it."
            // Dependency checks are deliberately excluded — a database outage should not trigger
            // a container restart, as that won't fix the database.
            app.MapHealthChecks(
                HealthCheckConstants.Endpoints.Live,
                new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains(HealthCheckConstants.Tags.Live),
                }
            );
        }

        return app;
    }
}
