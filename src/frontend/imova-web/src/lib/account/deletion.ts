// Pure rules for the "delete my account" panel (see components/account/DeleteAccountSection).

// What the account holds — GET /api/v1/users/me/data-summary.
export type AccountDataSummary = {
  listings: number;
  activeListings: number;
  favorites: number;
  savedSearches: number;
  conversations: number;
  hasAgency: boolean;
  hasPassword: boolean;
};

// One line of "this will be deleted" — `key` is the Account.deletion.* message, `count` its number.
export type DeletionItem = { key: "listings" | "favorites" | "savedSearches" | "agency"; count: number };

// Only what the account actually has, so the list reads as the user's own data, not boilerplate.
export function deletionItems(summary: AccountDataSummary): DeletionItem[] {
  const items: DeletionItem[] = [];
  if (summary.listings > 0) items.push({ key: "listings", count: summary.listings });
  if (summary.hasAgency) items.push({ key: "agency", count: 1 });
  if (summary.favorites > 0) items.push({ key: "favorites", count: summary.favorites });
  if (summary.savedSearches > 0) items.push({ key: "savedSearches", count: summary.savedSearches });
  return items;
}

// The final button stays disabled until the user has confirmed they understand — and, for an
// account with a password, typed it (the API checks it; this only avoids a pointless round trip).
export function canConfirmDeletion(input: { hasPassword: boolean; password: string; acknowledged: boolean }): boolean {
  if (!input.acknowledged) return false;
  return !input.hasPassword || input.password.length > 0;
}
