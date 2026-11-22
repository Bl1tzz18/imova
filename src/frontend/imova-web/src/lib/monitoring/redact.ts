// What the web server sends to Application Insights must not carry tokens or personal data that
// sit in addresses: reset/confirm/delete links (?token=…), unsubscribe links, searched places. Only
// the parameter names of a query string are kept, never the values — the same rule as the API's
// TelemetryRedaction (src/backend/Imova.Infrastructure/Monitoring).
export const REDACTED = "REDACTED";

// OpenTelemetry's current attribute names, the older ones some instrumentations still write, and
// Next.js's own copy of its span name ("GET /path?query").
export const URL_ATTRIBUTES = ["url.full", "url.query", "url.path", "http.url", "http.target", "next.span_name"] as const;

export function redactUrl(value: string): string {
  const queryStart = value.indexOf("?");
  if (queryStart < 0) return value;

  const fragmentStart = value.indexOf("#", queryStart);
  const query = fragmentStart < 0 ? value.slice(queryStart + 1) : value.slice(queryStart + 1, fragmentStart);
  const names = query
    .split("&")
    .filter((pair) => pair.length > 0)
    .map((pair) => `${pair.split("=")[0]}=${REDACTED}`);
  const fragment = fragmentStart < 0 ? "" : value.slice(fragmentStart);
  return `${value.slice(0, queryStart)}?${names.join("&")}${fragment}`;
}

// url.query holds just the query ("a=1&b=2"), the others a path or a full address.
export function redactAttribute(name: string, value: string): string {
  return name === "url.query" && !value.startsWith("?") ? redactUrl(`?${value}`).slice(1) : redactUrl(value);
}

// A finished span as the SDK keeps it: the name and attributes are plain fields we may rewrite
// before the batch is exported. Next.js renames its request span to "GET /path?query" late, after
// the span started — so this runs when a span ends, not when it starts.
export function redactSpan(span: { name: string; attributes: Record<string, unknown> }) {
  span.name = redactUrl(span.name);
  for (const name of URL_ATTRIBUTES) {
    const value = span.attributes[name];
    if (typeof value === "string") span.attributes[name] = redactAttribute(name, value);
  }
}
