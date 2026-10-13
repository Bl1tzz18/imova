import { describe, expect, it } from "vitest";
import { localeAfterSignIn } from "./signInLocale";

describe("localeAfterSignIn", () => {
  it("switches the site to the account's language when nothing was chosen on this device", () => {
    expect(localeAfterSignIn("ru", undefined)).toEqual({ locale: "ru", saveToAccount: false });
  });

  it("keeps a language chosen before signing in, and saves it on the account", () => {
    expect(localeAfterSignIn("ru", "en")).toEqual({ locale: "en", saveToAccount: true });
  });

  it("doesn't save when the chosen language is already the account's", () => {
    expect(localeAfterSignIn("en", "en")).toEqual({ locale: "en", saveToAccount: false });
  });

  it("ignores an unknown choice and falls back to Romanian for an unusable account value", () => {
    expect(localeAfterSignIn("ru", "de")).toEqual({ locale: "ru", saveToAccount: false });
    expect(localeAfterSignIn(undefined, undefined)).toEqual({ locale: "ro", saveToAccount: false });
  });
});
