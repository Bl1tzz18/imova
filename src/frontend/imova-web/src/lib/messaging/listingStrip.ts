import type { ConversationListingDetails } from "@/types/messaging";

// Whether the listing strip in a thread links to the listing page: always while it's live; once it
// isn't (archived, sold, …) only its publisher can still see the page — for the visitor it would 404.
export function canOpenListing(listing: ConversationListingDetails | null, viewerIsInitiator: boolean): boolean {
  return listing !== null && (listing.isActive || !viewerIsInitiator);
}
