import type { MyAgency } from "@/types/agency";

// "Publică ca": a private listing ("") or one of the user's agencies (its id). The last choice made
// on the create form is remembered in this browser only (localStorage), never on the account.
export const PUBLISH_AS_STORAGE_KEY = "imova:publishAs:v1";

export type PublishAsOption = { id: string; name: string; logoUrl: string | null; isVerified: boolean };

// What the picker offers besides "private": the active agencies the user belongs to, plus — when
// editing — the agency the listing is already under, even if it's no longer active (so saving
// without touching the picker never moves it). Empty: no picker at all.
export function publishAsOptions(agencies: MyAgency[], currentAgency?: PublishAsOption | null): PublishAsOption[] {
  const options = agencies
    .filter((a) => a.status === "Active")
    .map((a) => ({ id: a.id, name: a.name, logoUrl: a.logoThumbnailUrl, isVerified: a.isVerified }));
  if (currentAgency && !options.some((o) => o.id === currentAgency.id)) {
    options.unshift(currentAgency);
  }
  return options;
}

// Where the picker starts. Editing: wherever the listing is now. Creating: ?agencyId= when it's one
// of the options, else the remembered choice when it still is one ("" counts), else private.
export function initialPublishAs({
  options,
  editingAgencyId,
  requested,
  remembered,
}: {
  options: PublishAsOption[];
  editingAgencyId?: string | null;
  requested?: string | null;
  remembered?: string | null;
}): string {
  if (editingAgencyId !== undefined) return editingAgencyId ?? "";
  const isOption = (id: string | null | undefined) => id === "" || options.some((o) => o.id === id);
  if (requested && isOption(requested)) return requested;
  if (remembered != null && isOption(remembered)) return remembered;
  return "";
}

// Who may edit a listing, as the API decides it (ListingAccess): its author, a site admin, or an
// Owner/Admin of the agency it's under. Only the author may move it between private and an agency.
export function listingEditRights({
  userId,
  isSiteAdmin,
  authorUserId,
  listingAgencyId,
  agencies,
}: {
  userId: string;
  isSiteAdmin: boolean;
  authorUserId: string;
  listingAgencyId: string | null;
  agencies: MyAgency[];
}): { canEdit: boolean; canChangeAgency: boolean } {
  const isAuthor = userId === authorUserId;
  const managesAgency =
    listingAgencyId !== null && agencies.some((a) => a.id === listingAgencyId && (a.role === "Owner" || a.role === "Admin"));
  return { canEdit: isAuthor || isSiteAdmin || managesAgency, canChangeAgency: isAuthor };
}

// localStorage can be missing or throw (private windows, blocked storage): never fatal.
export function readRememberedPublishAs(): string | null {
  try {
    return window.localStorage.getItem(PUBLISH_AS_STORAGE_KEY);
  } catch {
    return null;
  }
}

export function rememberPublishAs(choice: string): void {
  try {
    window.localStorage.setItem(PUBLISH_AS_STORAGE_KEY, choice);
  } catch {
    // Only a convenience.
  }
}
