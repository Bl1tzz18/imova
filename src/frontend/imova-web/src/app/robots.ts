import type { MetadataRoute } from "next";
import { robotsRules } from "@/lib/seo/robots";
import { siteUrl } from "@/lib/site";

// /robots.txt (see lib/seo/robots.ts). Rendered per request: SITE_URL is the deployment's, not
// the build's.
export const dynamic = "force-dynamic";

export default function robots(): MetadataRoute.Robots {
  return robotsRules(siteUrl());
}
