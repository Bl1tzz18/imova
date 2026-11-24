// The phone's bottom tab bar (components/layout/MobileTabBar): which tab a page belongs to, and the
// pages that keep the bottom of the screen for their own controls. Paths come without the language
// prefix (usePathname from @/i18n/navigation).

export type Tab = "home" | "messages" | "add" | "saved" | "account";

// Pages with their own controls at the bottom: the add and edit forms (step buttons), a
// conversation and a first message (the writing box). A listing keeps the tab bar — its
// "Date de contact / Scrie mesaj" bar sits on top of it.
const OWN_BOTTOM_BAR = [/^\/properties\/new$/, /^\/my-listings\/[^/]+\/edit$/, /^\/messages\/.+/];

export function showsTabBar(pathname: string): boolean {
  return !OWN_BOTTOM_BAR.some((pattern) => pattern.test(normalize(pathname)));
}

export function activeTab(pathname: string): Tab | null {
  const path = normalize(pathname);
  if (path === "/") return "home";
  if (path === "/messages" || path.startsWith("/messages/")) return "messages";
  if (path === "/properties/new") return "add";
  if (path === "/saved-listings") return "saved";
  if (["/account", "/my-listings", "/saved-searches"].some((p) => path === p || path.startsWith(`${p}/`))) return "account";
  return null;
}

// The header's "where?" field on a phone: everywhere but the search and map pages, which have their
// own location field (it edits the current search instead of starting a new one).
export function showsHeaderSearch(pathname: string): boolean {
  const path = normalize(pathname);
  return path !== "/search" && path !== "/map";
}

function normalize(pathname: string): string {
  const path = pathname.split(/[?#]/)[0];
  return path.length > 1 ? path.replace(/\/+$/, "") : path;
}
