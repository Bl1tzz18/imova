import { logs } from "@opentelemetry/api-logs";
import type { PageView } from "./pageViews";

// A PageView (lib/monitoring/pageViews) as an Application Insights custom event — the Azure
// exporter turns a log record with "microsoft.custom_event.name" into a row of customEvents. Only
// with APPLICATIONINSIGHTS_CONNECTION_STRING (the logger is a no-op before telemetry starts, and
// telemetry only starts with it). Not sampled: the web app's log sampling doesn't follow its
// rate-limited request sampling (telemetry.ts), so every page view is counted.
export function recordPageView(view: PageView): void {
  if (!process.env.APPLICATIONINSIGHTS_CONNECTION_STRING) return;
  logs.getLogger("imova-web").emit({
    body: "PageView",
    attributes: {
      "microsoft.custom_event.name": "PageView",
      page: view.page,
      locale: view.locale,
      device: view.device,
      source: view.source,
      ...(view.utmSource && { utmSource: view.utmSource }),
      ...(view.utmMedium && { utmMedium: view.utmMedium }),
      ...(view.utmCampaign && { utmCampaign: view.utmCampaign }),
    },
  });
}
