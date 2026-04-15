/**
 * Liveness probe: verifies the Nitro process can handle HTTP requests.
 *
 * A failure here means the process is stuck or deadlocked — the orchestrator
 * should restart the container. Dependency checks are deliberately excluded:
 * a Valkey outage should not trigger a container restart.
 */
export default defineEventHandler(() => ({ status: "healthy" }));
