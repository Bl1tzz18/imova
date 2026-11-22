// The admin area: its sections (one tab each), and how many things wait in each — from
// GET /api/v1/admin/summary. The total is the count on the account menu's "Administrare" entry.
// Pure (Vitest-covered).

export type AdminSummary = {
  pendingReviewListings: number;
  reportedListings: number;
  openConversationReports: number;
  flaggedMessages: number;
};

export const ADMIN_SECTIONS = [
  { key: "moderation", href: "/admin/moderation", label: "moderationQueue" },
  { key: "messaging", href: "/admin/messaging", label: "messagingReports" },
  { key: "agencies", href: "/admin/agencies", label: "agencyVerification" },
  { key: "admins", href: "/admin/admins", label: "administrators" },
] as const;

export type AdminSectionKey = (typeof ADMIN_SECTIONS)[number]["key"];

// Where "Administrare" leads: the section with something waiting (listings first), else moderation.
export const ADMIN_HOME = "/admin/moderation";

export function adminSectionCount(summary: AdminSummary | null, key: AdminSectionKey): number {
  if (!summary) return 0;
  switch (key) {
    case "moderation":
      return summary.pendingReviewListings + summary.reportedListings;
    case "messaging":
      return summary.openConversationReports + summary.flaggedMessages;
    default:
      // Agencies and admins have nothing "waiting": an unverified agency may never ask.
      return 0;
  }
}

export function adminWaitingTotal(summary: AdminSummary | null): number {
  return ADMIN_SECTIONS.reduce((total, section) => total + adminSectionCount(summary, section.key), 0);
}

// "99+" past 99, so the badge stays small.
export function badgeText(count: number): string {
  return count > 99 ? "99+" : String(count);
}
