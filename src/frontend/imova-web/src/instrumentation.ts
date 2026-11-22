import type { Instrumentation } from "next";

// Application Insights for the web server (lib/monitoring/telemetry.ts) — Node.js runtime only, and
// only with APPLICATIONINSIGHTS_CONNECTION_STRING set (nothing is sent locally or in CI).
const connectionString = process.env.APPLICATIONINSIGHTS_CONNECTION_STRING;

export async function register() {
  if (process.env.NEXT_RUNTIME === "nodejs" && connectionString) {
    const { startTelemetry } = await import("./lib/monitoring/telemetry");
    startTelemetry(connectionString);
  }
}

export const onRequestError: Instrumentation.onRequestError = async (error, request, context) => {
  if (process.env.NEXT_RUNTIME === "nodejs" && connectionString) {
    const { recordRequestError } = await import("./lib/monitoring/telemetry");
    recordRequestError(error, request, context);
  }
};
