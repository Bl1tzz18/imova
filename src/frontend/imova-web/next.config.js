const createNextIntlPlugin = require("next-intl/plugin");

const withNextIntl = createNextIntlPlugin();

/** @type {import('next').NextConfig} */
const nextConfig = {
  // Self-contained server (only the node_modules it actually uses) — keeps the Docker image small.
  output: "standalone",
};

module.exports = withNextIntl(nextConfig);
