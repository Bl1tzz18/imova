import { describe, expect, it } from "vitest";
import { shareHref, shareText } from "./share";

const url = "https://imova.md/property/abc";
const text = "Apartament 2 camere · 82 000 € · Chișinău, Botanica";

describe("shareText", () => {
  it("joins what's there with dots", () => {
    expect(shareText(["Apartament 2 camere", "82 000 €", null, " ", "Chișinău, Botanica"])).toBe(text);
  });
});

describe("shareHref", () => {
  it("puts the text and link in one message for WhatsApp and Viber", () => {
    expect(shareHref("whatsapp", url, text)).toBe(`https://wa.me/?text=${encodeURIComponent(`${text}\n${url}`)}`);
    expect(shareHref("viber", url, text)).toBe(`viber://forward?text=${encodeURIComponent(`${text}\n${url}`)}`);
  });

  it("gives Telegram the link and the text separately, Facebook only the link", () => {
    expect(shareHref("telegram", url, text)).toBe(
      `https://t.me/share/url?url=${encodeURIComponent(url)}&text=${encodeURIComponent(text)}`,
    );
    expect(shareHref("facebook", url, text)).toBe(`https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(url)}`);
  });
});
