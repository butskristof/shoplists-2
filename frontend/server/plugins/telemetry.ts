/**
 * OpenTelemetry instrumentation for the Nitro server.
 *
 * Initializes the OpenTelemetry Node.js SDK at module load time (before Nitro
 * handles any requests) so that auto-instrumentation can patch Node.js built-in
 * modules (http, fetch, dns, etc.) early enough to capture all server-side traffic.
 *
 * ## What gets instrumented automatically
 *
 * `getNodeAutoInstrumentations()` is a meta-package that patches ~40 Node.js
 * modules. The most relevant ones for this app:
 * - **http/undici** — incoming Nitro requests + outgoing BFF calls to the backend API
 * - **ioredis** — Valkey operations (OIDC session/token storage)
 * - **dns** — DNS lookups
 * - **runtime metrics** — event loop delay, GC, V8 heap (via @opentelemetry/instrumentation-runtime-node)
 *
 * ## Configuration via environment variables
 *
 * The SDK reads all configuration from standard OpenTelemetry environment variables.
 * Aspire injects these automatically for resources added via `AddJavaScriptApp()`:
 *
 * - `OTEL_EXPORTER_OTLP_ENDPOINT` — OTLP collector URL (e.g. http://localhost:4317).
 *   All three exporters (trace, metric, log) use this as their target.
 * - `OTEL_SERVICE_NAME` — identifies this resource in the Aspire dashboard (e.g. "frontend").
 * - `OTEL_RESOURCE_ATTRIBUTES` — additional resource attributes.
 *
 * No hardcoded endpoints or service names — the same code works in local dev (Aspire)
 * and production (any OTLP-compatible collector) by setting these env vars.
 *
 * ## Signals exported
 *
 * - **Traces** — exported via gRPC using `OTLPTraceExporter`
 * - **Metrics** — exported via gRPC using `OTLPMetricExporter` on a periodic interval
 * - **Logs** — pipeline is wired (`BatchLogRecordProcessor` + `OTLPLogExporter`) but
 *   currently no log records are emitted. Nitro uses consola for logging, which OTel
 *   does not auto-instrument. A custom consola reporter could bridge logs into this
 *   pipeline in the future if structured log export becomes needed.
 *
 * ## Why the empty defineNitroPlugin export
 *
 * Nitro requires server plugins to have a default export via `defineNitroPlugin()`.
 * The actual SDK initialization happens at module scope (above the export) so it runs
 * before any request handling. The empty plugin body is intentional.
 */
import { getNodeAutoInstrumentations } from "@opentelemetry/auto-instrumentations-node";
import { OTLPLogExporter } from "@opentelemetry/exporter-logs-otlp-grpc";
import { OTLPMetricExporter } from "@opentelemetry/exporter-metrics-otlp-grpc";
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-grpc";
import { BatchLogRecordProcessor } from "@opentelemetry/sdk-logs";
import { PeriodicExportingMetricReader } from "@opentelemetry/sdk-metrics";
import { NodeSDK } from "@opentelemetry/sdk-node";

const sdk = new NodeSDK({
  traceExporter: new OTLPTraceExporter(),
  metricReaders: [new PeriodicExportingMetricReader({
    exporter: new OTLPMetricExporter(),
  })],
  logRecordProcessors: [
    new BatchLogRecordProcessor(new OTLPLogExporter()),
  ],
  instrumentations: [getNodeAutoInstrumentations()],
});

sdk.start();

export default defineNitroPlugin(() => {});
