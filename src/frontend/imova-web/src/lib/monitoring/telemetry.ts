import { useAzureMonitor } from "@azure/monitor-opentelemetry";
import { SpanStatusCode, trace } from "@opentelemetry/api";
import { redactSpan, redactUrl } from "./redact";

// The web server's requests, its calls to the API and its errors go to Azure Application Insights
// (cloud role "imova-web") — only when APPLICATIONINSIGHTS_CONNECTION_STRING is set, so local runs,
// compose and CI send nothing. Server-side only, on purpose: the browser SDK would set cookies (see
// the privacy policy's cookie table). Loaded from instrumentation.ts in the Node.js runtime.
export function startTelemetry(connectionString: string) {
  process.env.OTEL_SERVICE_NAME ??= "imova-web";
  process.env.OTEL_RESOURCE_ATTRIBUTES ??= `service.version=${process.env.APP_VERSION ?? "dev"}`;
  // The SDK's own usage reports ("Statsbeat") go to Microsoft's endpoint whatever our connection
  // string says — off, so telemetry only goes where we send it. The first variable turns off only
  // the "non-essential" part; the legacy one turns off the rest.
  process.env.APPLICATIONINSIGHTS_STATSBEAT_DISABLED ??= "true";
  process.env.APPLICATION_INSIGHTS_NO_STATSBEAT ??= "true";

  useAzureMonitor({
    azureMonitorExporterOptions: { connectionString },
    spanProcessors: [redactingProcessor],
    // A live stream to a separate Microsoft endpoint; not needed at this size.
    enableLiveMetrics: false,
  });
}

// Strips query values from every span's name and address attributes before export (redactSpan).
// Structural, so no direct dependency on the SDK package.
const redactingProcessor = {
  onStart() {},
  onEnd(span: unknown) {
    redactSpan(span as Parameters<typeof redactSpan>[0]);
  },
  forceFlush: () => Promise.resolve(),
  shutdown: () => Promise.resolve(),
};

// A server-side render/route/action error (Next's onRequestError) as an exception in Application
// Insights, with the route but not the query string.
export function recordRequestError(
  error: unknown,
  request: { path: string; method: string },
  context: { routePath: string; routeType: string },
) {
  const span = trace.getTracer("imova-web").startSpan("request error", {
    attributes: {
      "http.request.method": request.method,
      "url.path": redactUrl(request.path),
      "http.route": context.routePath,
      "next.route_type": context.routeType,
    },
  });
  span.recordException(error instanceof Error ? error : String(error));
  span.setStatus({ code: SpanStatusCode.ERROR });
  span.end();
}
