// The admin moderation page's tabs (?tab=…): three lists of listings by status, and the reports
// visitors filed on listings (?tab=reports, with ?view=open|resolved).
export const MODERATION_TABS = ["pending", "active", "suspended", "reports"] as const;

export type ModerationTab = (typeof MODERATION_TABS)[number];

// The tabs that list listings by status (the reports tab lists report cases instead).
export type ListingTab = Exclude<ModerationTab, "reports">;

const STATUS: Record<ListingTab, "PendingReview" | "Active" | "Suspended"> = {
  pending: "PendingReview",
  active: "Active",
  suspended: "Suspended",
};

// Anything unknown (or missing) is the review queue.
export function parseModerationTab(value: string | undefined): ModerationTab {
  return MODERATION_TABS.find((tab) => tab === value) ?? "pending";
}

export function isListingTab(tab: ModerationTab): tab is ListingTab {
  return tab !== "reports";
}

export function statusForTab(tab: ListingTab) {
  return STATUS[tab];
}

// The reports tab: cases still waiting for a decision, or the decisions already made.
export const REPORT_VIEWS = ["open", "resolved"] as const;

export type ReportView = (typeof REPORT_VIEWS)[number];

export function parseReportView(value: string | undefined): ReportView {
  return value === "resolved" ? "resolved" : "open";
}

export const MODERATION_PAGE_SIZE = 20;

// A link within the moderation page. Only what differs from the defaults goes into the URL
// (the review queue, no search, page 1, open reports).
export function moderationHref({
  tab,
  q,
  page,
  view,
}: {
  tab: ModerationTab;
  q?: string;
  page?: number;
  view?: ReportView;
}): string {
  const params = new URLSearchParams();
  if (tab !== "pending") params.set("tab", tab);
  if (tab === "reports" && view === "resolved") params.set("view", view);
  const search = isListingTab(tab) ? q?.trim() : undefined;
  if (search) params.set("q", search);
  if (page && page > 1) params.set("page", String(page));
  const query = params.toString();
  return query ? `/admin/moderation?${query}` : "/admin/moderation";
}

// ?page=… as a page number: anything missing or not a positive whole number is page 1.
export function parsePage(value: string | undefined): number {
  const page = Number(value);
  return Number.isInteger(page) && page > 0 ? page : 1;
}
