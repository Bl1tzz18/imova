import { parsePhoneNumberFromString } from "libphonenumber-js";
import type { ListingContact } from "@/types/listing";

// The listing page's contact card — pure rules (Vitest-covered): how the number is shown and
// half-hidden, the chat links for the messaging apps, and the line under the person's name.

const DEFAULT_COUNTRY = "MD";
const MASK = "•";
// Digits hidden until the visitor asks for the number: +373 687 •• •••.
const HIDDEN_DIGITS = 5;

// "+373 687 53 388" — the international format when the number parses, otherwise as entered.
export function formatPhone(phone: string): string {
  const parsed = parsePhoneNumberFromString(phone, DEFAULT_COUNTRY);
  return parsed?.isPossible() ? parsed.formatInternational() : phone.trim();
}

// The formatted number with its last digits replaced by dots — always at least 3 digits shown.
export function maskPhone(phone: string): string {
  const formatted = formatPhone(phone);
  const totalDigits = formatted.replace(/\D/g, "").length;
  const shown = Math.max(3, totalDigits - HIDDEN_DIGITS);
  let seen = 0;
  return formatted.replace(/\d/g, (digit) => (++seen <= shown ? digit : MASK));
}

// Digits only, country code first, no "+" — what the apps' chat links take. A local number
// ("069 123 456") is read as Moldovan.
export function internationalDigits(phone: string): string {
  const parsed = parsePhoneNumberFromString(phone, DEFAULT_COUNTRY);
  return parsed ? parsed.number.replace(/\D/g, "") : phone.replace(/\D/g, "");
}

export const MESSAGING_APP_ORDER = ["WhatsApp", "Viber", "Telegram"] as const;
export type MessagingApp = (typeof MESSAGING_APP_ORDER)[number];

export function isMessagingApp(value: string): value is MessagingApp {
  return (MESSAGING_APP_ORDER as readonly string[]).includes(value);
}

// Opens a chat with the number in the app. WhatsApp and Telegram are web links (they hand over to
// the app, or the web version on a computer); Viber only has its own scheme.
export function messagingAppHref(app: MessagingApp, phone: string): string {
  const digits = internationalDigits(phone);
  switch (app) {
    case "WhatsApp":
      return `https://wa.me/${digits}`;
    case "Viber":
      return `viber://chat?number=%2B${digits}`;
    case "Telegram":
      return `https://t.me/+${digits}`;
  }
}

// Whether the app's link opens a web page (a new tab) rather than the app itself.
export function opensInNewTab(app: MessagingApp): boolean {
  return app !== "Viber";
}

// The line under the name: an agency's agent ("Agent imobiliar · Casa Ta"), the agency itself
// (while the account behind it has no name of its own), an individual owner, or the separate
// contact person an individual named.
export type ContactRole =
  | { kind: "agent"; agencyName: string }
  | { kind: "agency" }
  | { kind: "individual" }
  | { kind: "contactPerson" };

export function contactRole(contact: Pick<ListingContact, "personType" | "name" | "agencyName">): ContactRole {
  if (contact.agencyName) {
    return contact.name === contact.agencyName ? { kind: "agency" } : { kind: "agent", agencyName: contact.agencyName };
  }
  return contact.personType === "Other" ? { kind: "contactPerson" } : { kind: "individual" };
}
