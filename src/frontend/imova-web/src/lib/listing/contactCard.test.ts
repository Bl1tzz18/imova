import { describe, expect, it } from "vitest";
import {
  contactRole,
  formatPhone,
  internationalDigits,
  glyphText,
  maskedPhoneGlyphs,
  messagingAppHref,
  opensInNewTab,
  revealedPhoneGlyphs,
} from "./contactCard";

describe("formatPhone", () => {
  it("formats a Moldovan number internationally, however it was typed", () => {
    expect(formatPhone("+37368753388")).toBe("+373 687 53 388");
    expect(formatPhone("+373 68 753 388")).toBe("+373 687 53 388");
    expect(formatPhone("068753388")).toBe("+373 687 53 388");
  });

  it("keeps a number it can't read as entered", () => {
    expect(formatPhone(" 12 ")).toBe("12");
  });
});

describe("phone glyphs", () => {
  it("draws the masked number in the real number's format, its last digits as dots", () => {
    expect(glyphText(maskedPhoneGlyphs("+373687", 5))).toBe("+373 687 •• •••");
    expect(glyphText(maskedPhoneGlyphs("+373225", 5))).toBe("+373 22 5•• •••");
  });

  it("reveals the same digits in the same places, so only they change", () => {
    const masked = maskedPhoneGlyphs("+373687", 5);
    const revealed = revealedPhoneGlyphs("+37368753388", 5);

    expect(glyphText(revealed)).toBe("+373 687 53 388");
    expect(revealed.length).toBe(masked.length);
    expect(revealed.map((g) => g.hidden)).toEqual(masked.map((g) => g.hidden));
    expect(revealed.filter((g) => g.hidden).map((g) => g.char).join("")).toBe("53388");
  });

  it("never hides more digits than there are", () => {
    expect(glyphText(maskedPhoneGlyphs("123", 0))).toBe("123");
  });
});

describe("internationalDigits", () => {
  it("is the country code and number, digits only", () => {
    expect(internationalDigits("+373 68 753 388")).toBe("37368753388");
    expect(internationalDigits("068 753 388")).toBe("37368753388");
    expect(internationalDigits("+40 721 234 567")).toBe("40721234567");
  });
});

describe("messagingAppHref", () => {
  it("opens a chat with the number in each app", () => {
    expect(messagingAppHref("WhatsApp", "+373 68 753 388")).toBe("https://wa.me/37368753388");
    expect(messagingAppHref("Viber", "+373 68 753 388")).toBe("viber://chat?number=%2B37368753388");
    expect(messagingAppHref("Telegram", "+373 68 753 388")).toBe("https://t.me/+37368753388");
  });

  it("only the web links open a new tab", () => {
    expect(opensInNewTab("WhatsApp")).toBe(true);
    expect(opensInNewTab("Telegram")).toBe(true);
    expect(opensInNewTab("Viber")).toBe(false);
  });
});

describe("contactRole", () => {
  it("is an agent of the agency when the person isn't the agency itself", () => {
    expect(contactRole({ personType: "Self", name: "Elena Ciobanu", agencyName: "Casa Ta" })).toEqual({
      kind: "agent",
      agencyName: "Casa Ta",
    });
    expect(contactRole({ personType: "Other", name: "Maria", agencyName: "Casa Ta" })).toEqual({
      kind: "agent",
      agencyName: "Casa Ta",
    });
  });

  it("is the agency when the name is the agency's", () => {
    expect(contactRole({ personType: "Self", name: "Casa Ta", agencyName: "Casa Ta" })).toEqual({ kind: "agency" });
  });

  it("is an individual owner, or the contact person they named", () => {
    expect(contactRole({ personType: "Self", name: "Ion", agencyName: null })).toEqual({ kind: "individual" });
    expect(contactRole({ personType: "Other", name: "Maria", agencyName: null })).toEqual({ kind: "contactPerson" });
  });
});
