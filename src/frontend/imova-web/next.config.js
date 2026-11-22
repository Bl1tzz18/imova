const createNextIntlPlugin = require("next-intl/plugin");

const withNextIntl = createNextIntlPlugin();

/** @type {import('next').NextConfig} */
const nextConfig = {
  // Self-contained server (only the node_modules it actually uses) — keeps the Docker image small.
  output: "standalone",
  // Application Insights (lib/monitoring/telemetry.ts): loaded as plain Node modules, not bundled —
  // OpenTelemetry patches Node's own modules (http) at runtime, and there must be one copy of
  // @opentelemetry/api (pinned to the SDK's version in package.json), or our spans go nowhere.
  serverExternalPackages: ["@azure/monitor-opentelemetry", "@opentelemetry/api"],
};

module.exports = withNextIntl(nextConfig);
