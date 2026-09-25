import { describe, expect, it } from "vitest";
import { messageButtonEmphasis, messageButtonHref, showsRelayNotice } from "@/lib/messaging/contact";
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
