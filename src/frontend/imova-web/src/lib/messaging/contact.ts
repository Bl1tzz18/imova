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

// The phone-only action bar pinned to the bottom of a listing page: "Date de contact" (opens the
// contact card — who, the number, apps, call hours) and "Scrie mesaj". The highlighted one follows
// the owner: messages when they asked for platform messages or hid the number, the contact
// details otherwise. The owner sees no bar on their own listing (null); a listing without contact
// details (from before the Contact step) only offers messages.
export type MobileContactBar = { contactDetails: boolean; primary: "contactDetails" | "message" };

export function mobileContactBar(contact: ListingContact | null | undefined, viewerIsOwner: boolean): MobileContactBar | null {
  if (viewerIsOwner) return null;
  if (!contact) return { contactDetails: false, primary: "message" };
  const prefersMessages = contact.preferredContactMethod === "PlatformMessages" || contact.hidePhoneNumber || !contact.phone;
  return { contactDetails: true, primary: prefersMessages ? "message" : "contactDetails" };
}
