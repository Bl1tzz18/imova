import { describe, expect, it } from "vitest";
import { messageButtonEmphasis, messageButtonHref, mobileContactBar, showsRelayNotice } from "@/lib/messaging/contact";
import type { ListingContact } from "@/types/listing";

const contact = (extra: Partial<ListingContact> = {}): ListingContact => ({
  personType: "Self",
  name: "Ion",
  phone: "+37369111222",
  email: "ion@example.com",
  messagingApps: [],
  preferredContactMethod: "Any",
  hidePhoneNumber: false,
  callHoursFrom: null,
  callHoursTo: null,
  pictureUrl: null,
  agencyName: null,
  phonePrefix: null,
  phoneHiddenDigits: null,
  ...extra,
});

describe("messageButtonEmphasis", () => {
  it("makes messaging the main option when the phone is hidden", () => {
    expect(messageButtonEmphasis(contact({ hidePhoneNumber: true, phone: null }))).toBe("primary");
  });

  it("keeps it secondary next to a visible phone", () => {
    expect(messageButtonEmphasis(contact())).toBe("secondary");
  });

  it("is the main option when there's no phone at all", () => {
    expect(messageButtonEmphasis(contact({ phone: null }))).toBe("primary");
    expect(messageButtonEmphasis(null)).toBe("primary");
  });
});

describe("showsRelayNotice", () => {
  it("tells the owner that messages come to them, not to the Other contact", () => {
    expect(showsRelayNotice(contact({ personType: "Other" }), true)).toBe(true);
  });

  it("isn't shown to visitors, or when the owner is the contact", () => {
    expect(showsRelayNotice(contact({ personType: "Other" }), false)).toBe(false);
    expect(showsRelayNotice(contact(), true)).toBe(false);
  });
});

describe("messageButtonHref", () => {
  it("asks an anonymous visitor to log in, then comes back to the compose page", () => {
    expect(messageButtonHref("L1", false, null)).toBe("/login?next=%2Fmessages%2Fnew%3Flisting%3DL1");
  });

  it("opens an existing conversation, or the compose page", () => {
    expect(messageButtonHref("L1", true, "C9")).toBe("/messages/C9");
    expect(messageButtonHref("L1", true, null)).toBe("/messages/new?listing=L1");
  });
});

describe("mobileContactBar", () => {
  it("highlights the contact details when the number is public", () => {
    expect(mobileContactBar(contact(), false)).toEqual({ contactDetails: true, primary: "contactDetails" });
  });

  it("highlights messages when the owner prefers them or hid the number", () => {
    expect(mobileContactBar(contact({ preferredContactMethod: "PlatformMessages" }), false)).toEqual({ contactDetails: true, primary: "message" });
    expect(mobileContactBar(contact({ hidePhoneNumber: true, phone: null }), false)).toEqual({ contactDetails: true, primary: "message" });
  });

  it("offers only messages for a listing without contact details", () => {
    expect(mobileContactBar(null, false)).toEqual({ contactDetails: false, primary: "message" });
  });

  it("shows nothing to the owner", () => {
    expect(mobileContactBar(contact(), true)).toBeNull();
  });
});

describe("hasPhone", () => {
  const base = { phone: null, phonePrefix: null } as unknown as Parameters<typeof messageButtonEmphasis>[0];

  it("counts the number's shape the public gets as a number to call", () => {
    expect(messageButtonEmphasis({ ...base!, hidePhoneNumber: false, phonePrefix: "+373691", phoneHiddenDigits: 5 })).toBe("secondary");
    expect(messageButtonEmphasis({ ...base!, hidePhoneNumber: false })).toBe("primary");
  });
});
