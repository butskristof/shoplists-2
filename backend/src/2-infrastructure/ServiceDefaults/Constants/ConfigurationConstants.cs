using Microsoft.Extensions.Configuration;

namespace Shoplists.ServiceDefaults.Constants;

internal static class ConfigurationConstants
{
    internal const string OtelExporterOtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";

    internal static bool HasValue(this IConfiguration configuration, string key) =>
        !string.IsNullOrWhiteSpace(configuration[key]);
}
