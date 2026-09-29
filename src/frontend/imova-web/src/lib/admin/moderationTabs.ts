// The admin moderation page's tabs (?tab=…) and the listing status each one lists.
export const MODERATION_TABS = ["pending", "active", "suspended"] as const;

export type ModerationTab = (typeof MODERATION_TABS)[number];

const STATUS: Record<ModerationTab, "PendingReview" | "Active" | "Suspended"> = {
  pending: "PendingReview",
  active: "Active",
  suspended: "Suspended",
};

// Anything unknown (or missing) is the review queue.
export function parseModerationTab(value: string | undefined): ModerationTab {
  return MODERATION_TABS.find((tab) => tab === value) ?? "pending";
}

export function statusForTab(tab: ModerationTab) {
  return STATUS[tab];
}

export const MODERATION_PAGE_SIZE = 20;

// A link within the moderation page. Only what differs from the defaults goes into the URL
// (the review queue, no search, page 1).
export function moderationHref({ tab, q, page }: { tab: ModerationTab; q?: string; page?: number }): string {
  const params = new URLSearchParams();
  if (tab !== "pending") params.set("tab", tab);
  const search = q?.trim();
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
