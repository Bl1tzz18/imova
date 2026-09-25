import type { Listing, ListingContact, Publisher } from "@/types/listing";

export { isValidPhone } from "@/lib/utils/phone";

// The listing form's Contact step — pure rules shared by StepContact and the submit payload
// (formPayload.ts). Mirrors Imova.Domain.Listings.ListingContact / ListingContactValidator.

export const CONTACT_PERSON_TYPES = ["Self", "Other"] as const;
export type ContactPersonType = (typeof CONTACT_PERSON_TYPES)[number];

export const MESSAGING_APPS = ["WhatsApp", "Viber", "Telegram"] as const;

export const CONTACT_METHODS = ["Any", "PhoneCall", "PlatformMessages"] as const;
export type ContactMethod = (typeof CONTACT_METHODS)[number];

// Same slots the backend accepts (ListingContact.CallHourSlots): every half hour, 06:00–23:00.
export const CALL_HOUR_SLOTS: readonly string[] = Array.from({ length: 35 }, (_, i) => {
  const halfHours = 12 + i;
  return `${String(Math.floor(halfHours / 2)).padStart(2, "0")}:${halfHours % 2 === 0 ? "00" : "30"}`;
});

// The "until" dropdown only offers times after the chosen start ("HH:mm" sorts as time).
export function callHourEndSlots(from: string): readonly string[] {
  return from ? CALL_HOUR_SLOTS.filter((slot) => slot > from) : CALL_HOUR_SLOTS;
}

export function contactInputName(field: string): string {
  return `contact.${field}`;
}

// A hidden number can only be reached through platform messages — the method is forced to match.
export function effectiveContactMethod(hidePhoneNumber: boolean, method: ContactMethod): ContactMethod {
  return hidePhoneNumber ? "PlatformMessages" : method;
}

// Call hours only mean something while visitors may actually call.
export function callsAllowed(hidePhoneNumber: boolean, method: ContactMethod): boolean {
  return effectiveContactMethod(hidePhoneNumber, method) !== "PlatformMessages";
}

function text(formData: FormData, field: string): string | null {
  const value = formData.get(contactInputName(field));
  return typeof value === "string" && value.trim() !== "" ? value.trim() : null;
}

function asMethod(value: string | null): ContactMethod {
  return CONTACT_METHODS.find((m) => m === value) ?? "Any";
}

// Only sends what applies: a Self contact has no name/email of its own (they're the publisher's),
// a hidden number has no messaging apps, and call hours need calls to be allowed.
export function readContact(formData: FormData) {
  const personType: ContactPersonType = text(formData, "personType") === "Other" ? "Other" : "Self";
  const hidePhoneNumber = formData.get(contactInputName("hidePhoneNumber")) === "true";
  const method = effectiveContactMethod(hidePhoneNumber, asMethod(text(formData, "preferredContactMethod")));

  return {
    personType,
    phone: text(formData, "phone") ?? "",
    name: personType === "Other" ? text(formData, "name") : null,
    email: personType === "Other" ? text(formData, "email") : null,
    messagingApps: hidePhoneNumber
      ? []
      : MESSAGING_APPS.filter((app) => formData.getAll(contactInputName("messagingApps")).includes(app)),
    preferredContactMethod: method,
    hidePhoneNumber,
    callHoursFrom: callsAllowed(hidePhoneNumber, method) ? text(formData, "callHoursFrom") : null,
    callHoursTo: callsAllowed(hidePhoneNumber, method) ? text(formData, "callHoursTo") : null,
  };
}

export type ContactDefaults = {
  personType: ContactPersonType;
  // The listing publisher's own identity — what "Eu" (Self) shows.
  selfName: string | null;
  selfEmail: string | null;
  selfPhone: string;
  otherName: string;
  otherPhone: string;
  otherEmail: string;
  messagingApps: string[];
  preferredContactMethod: ContactMethod;
  hidePhoneNumber: boolean;
  callHoursFrom: string;
  callHoursTo: string;
};

// Pre-fills the step: an existing listing's saved contact when editing, otherwise "Eu" with the
// chosen publisher's details (a Self contact starts from the account's phone, still editable).
export function contactDefaults(listing: Listing | undefined, publisher: Publisher | undefined): ContactDefaults {
  const saved: ListingContact | null | undefined = listing?.contact;
  const savedSelf = saved?.personType === "Self" ? saved : null;
  const savedOther = saved?.personType === "Other" ? saved : null;

  return {
    personType: savedOther ? "Other" : "Self",
    selfName: savedSelf?.name ?? publisher?.displayName ?? listing?.publisher.displayName ?? null,
    selfEmail: savedSelf?.email ?? publisher?.email ?? null,
    selfPhone: savedSelf?.phone ?? publisher?.phone ?? "",
    otherName: savedOther?.name ?? "",
    otherPhone: savedOther?.phone ?? "",
    otherEmail: savedOther?.email ?? "",
    messagingApps: saved?.messagingApps ?? [],
    preferredContactMethod: asMethod(saved?.preferredContactMethod ?? null),
    hidePhoneNumber: saved?.hidePhoneNumber ?? false,
    callHoursFrom: saved?.callHoursFrom ?? "",
    callHoursTo: saved?.callHoursTo ?? "",
  };
}
