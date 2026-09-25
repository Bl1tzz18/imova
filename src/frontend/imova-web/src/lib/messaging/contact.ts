import type { ListingContact } from "@/types/listing";

// How prominent "Scrie mesaj" is on a listing: the main contact option when the phone number is
// hidden (visitors can't call or use WhatsApp/Viber/Telegram then), otherwise a secondary one next
// to the phone.
export function messageButtonEmphasis(contact: ListingContact | null | undefined): "primary" | "secondary" {
  return contact?.hidePhoneNumber || !contact?.phone ? "primary" : "secondary";
}

// Platform messages always reach the listing's publisher — never the "Other" contact person,
// who has no account. Their publisher is told so, to relay messages if needed.
export function showsRelayNotice(contact: ListingContact | null | undefined, viewerIsOwner: boolean): boolean {
  return viewerIsOwner && contact?.personType === "Other";
}

// Where "Scrie mesaj" leads: straight to an existing conversation, to the compose page, or to
// login first (coming back to the compose page afterwards).
export function messageButtonHref(listingId: string, loggedIn: boolean, existingConversationId: string | null): string {
  const compose = `/messages/new?listing=${encodeURIComponent(listingId)}`;
  if (!loggedIn) return `/login?next=${encodeURIComponent(compose)}`;
  return existingConversationId ? `/messages/${existingConversationId}` : compose;
}
