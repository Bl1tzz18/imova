import { describe, expect, it } from "vitest";
import {
  CALL_HOUR_SLOTS,
  callHourEndSlots,
  callsAllowed,
  contactDefaults,
  contactInputName,
  effectiveContactMethod,
  isValidPhone,
  readContact,
} from "@/lib/property/contact";
import type { Listing, Publisher } from "@/types/listing";

function form(values: Record<string, string | string[]>) {
  const data = new FormData();
  for (const [field, value] of Object.entries(values)) {
    for (const v of Array.isArray(value) ? value : [value]) data.append(contactInputName(field), v);
  }
  return data;
}

const publisher: Publisher = {
  id: "p1",
  userId: "u1",
  publisherType: "Individual",
  displayName: "Ion Popescu",
  phone: "+37369123456",
  email: "ion@example.com",
  logoUrl: null,
  bio: null,
};

describe("isValidPhone", () => {
  it.each(["+37369123456", "069 123 456", "+40 (721) 123-456"])("accepts %s", (phone) => {
    expect(isValidPhone(phone)).toBe(true);
  });

  it.each(["", "abc", "+373", "123456", "+373 69 123 456 789 000 111"])("rejects %j", (phone) => {
    expect(isValidPhone(phone)).toBe(false);
  });
});

describe("hiding the phone number", () => {
  it("forces platform messages as the contact method", () => {
    expect(effectiveContactMethod(true, "PhoneCall")).toBe("PlatformMessages");
    expect(effectiveContactMethod(true, "Any")).toBe("PlatformMessages");
    expect(effectiveContactMethod(false, "PhoneCall")).toBe("PhoneCall");
  });

  it("means no calls, so no call hours", () => {
    expect(callsAllowed(true, "Any")).toBe(false);
    expect(callsAllowed(false, "PlatformMessages")).toBe(false);
    expect(callsAllowed(false, "PhoneCall")).toBe(true);
    expect(callsAllowed(false, "Any")).toBe(true);
  });
});

describe("call hour dropdowns", () => {
  it("offer every half hour from 06:00 to 23:00, like the backend", () => {
    expect(CALL_HOUR_SLOTS).toHaveLength(35);
    expect(CALL_HOUR_SLOTS.slice(0, 3)).toEqual(["06:00", "06:30", "07:00"]);
    expect(CALL_HOUR_SLOTS.at(-1)).toBe("23:00");
  });

  it("only offer end times after the chosen start", () => {
    expect(callHourEndSlots("22:00")).toEqual(["22:30", "23:00"]);
    expect(callHourEndSlots("")).toBe(CALL_HOUR_SLOTS);
  });
});

describe("readContact", () => {
  it("sends a Self contact with only its phone — name and email are the publisher's", () => {
    const contact = readContact(
      form({ personType: "Self", phone: "+37369111222", name: "Ignored", email: "ignored@example.com", messagingApps: ["Viber"] }),
    );

    expect(contact).toEqual({
      personType: "Self",
      phone: "+37369111222",
      name: null,
      email: null,
      messagingApps: ["Viber"],
      preferredContactMethod: "Any",
      hidePhoneNumber: false,
      callHoursFrom: null,
      callHoursTo: null,
    });
  });

  it("sends another person's own name, phone and email", () => {
    const contact = readContact(
      form({ personType: "Other", name: " Maria ", phone: "+37379333444", email: "maria@example.com", preferredContactMethod: "PhoneCall", callHoursFrom: "09:00", callHoursTo: "18:00" }),
    );

    expect(contact).toMatchObject({
      personType: "Other",
      name: "Maria",
      phone: "+37379333444",
      email: "maria@example.com",
      preferredContactMethod: "PhoneCall",
      callHoursFrom: "09:00",
      callHoursTo: "18:00",
    });
  });

  it("leaves a blank optional email out for another person", () => {
    expect(readContact(form({ personType: "Other", name: "Maria", phone: "+37379333444", email: " " })).email).toBeNull();
  });

  it("forces platform messages and drops apps and call hours when the phone is hidden", () => {
    const contact = readContact(
      form({
        personType: "Self",
        phone: "+37369111222",
        hidePhoneNumber: "true",
        preferredContactMethod: "PhoneCall",
        messagingApps: ["WhatsApp", "Telegram"],
        callHoursFrom: "09:00",
        callHoursTo: "18:00",
      }),
    );

    expect(contact.hidePhoneNumber).toBe(true);
    expect(contact.preferredContactMethod).toBe("PlatformMessages");
    expect(contact.messagingApps).toEqual([]);
    expect(contact.callHoursFrom).toBeNull();
    expect(contact.callHoursTo).toBeNull();
  });

  it("defaults to Self and Any, and ignores unknown values", () => {
    const contact = readContact(form({ phone: "+37369111222", preferredContactMethod: "Pigeon", messagingApps: ["Signal"] }));

    expect(contact.personType).toBe("Self");
    expect(contact.preferredContactMethod).toBe("Any");
    expect(contact.messagingApps).toEqual([]);
  });
});

describe("contactDefaults", () => {
  it("starts a new listing as Self with the publisher's name, email and phone", () => {
    expect(contactDefaults(undefined, publisher)).toMatchObject({
      personType: "Self",
      selfName: "Ion Popescu",
      selfEmail: "ion@example.com",
      selfPhone: "+37369123456",
      preferredContactMethod: "Any",
      hidePhoneNumber: false,
    });
  });

  it("pre-fills an edited listing from its saved Other contact", () => {
    const listing = {
      publisher: { ...publisher, phone: null, email: null },
      contact: {
        personType: "Other",
        name: "Maria",
        phone: "+37379333444",
        email: null,
        messagingApps: ["WhatsApp"],
        preferredContactMethod: "PlatformMessages",
        hidePhoneNumber: true,
        callHoursFrom: null,
        callHoursTo: null,
      },
    } as unknown as Listing;

    expect(contactDefaults(listing, undefined)).toMatchObject({
      personType: "Other",
      otherName: "Maria",
      otherPhone: "+37379333444",
      otherEmail: "",
      selfName: "Ion Popescu",
      messagingApps: ["WhatsApp"],
      preferredContactMethod: "PlatformMessages",
      hidePhoneNumber: true,
    });
  });
});
