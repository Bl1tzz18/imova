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

// The phone-only action bar pinned to the bottom of a listing page: "Sună" (only when the number
// is public) and "Scrie mesaj". The main one follows the owner's preference — messages first when
// they asked for platform messages or there's no number to call. The owner sees no bar on their
// own listing (null).
export type MobileContactBar = { phone: string | null; primary: "call" | "message" };

export function mobileContactBar(contact: ListingContact | null | undefined, viewerIsOwner: boolean): MobileContactBar | null {
  if (viewerIsOwner) return null;
  const phone = contact?.hidePhoneNumber ? null : (contact?.phone ?? null);
  const primary = !phone || contact?.preferredContactMethod === "PlatformMessages" ? "message" : "call";
  return { phone, primary };
}
