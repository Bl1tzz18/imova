const createNextIntlPlugin = require("next-intl/plugin");

const withNextIntl = createNextIntlPlugin();

/** @type {import('next').NextConfig} */
const nextConfig = {
  // Search lives at /cauta now; old /search links (bookmarks, shared URLs) keep working.
  async redirects() {
    return [{ source: "/search", destination: "/cauta", permanent: true }];
  },
};

module.exports = withNextIntl(nextConfig);
