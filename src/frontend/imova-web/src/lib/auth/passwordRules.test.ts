import { describe, expect, it } from "vitest";
import { confirmationState, meetsPasswordRules, passwordChecks } from "./passwordRules";

describe("passwordChecks", () => {
  it("reports each rule separately as the password grows", () => {
    expect(passwordChecks("")).toEqual({ minLength: false, number: false, special: false });
    expect(passwordChecks("parola")).toEqual({ minLength: false, number: false, special: false });
    expect(passwordChecks("parola12")).toEqual({ minLength: true, number: true, special: false });
    expect(passwordChecks("parola12!")).toEqual({ minLength: true, number: true, special: true });
  });

  it("counts any non-letter, non-digit as special — spaces included, like the backend", () => {
    expect(passwordChecks("a b").special).toBe(true);
    expect(passwordChecks("ș").special).toBe(false);
    expect(passwordChecks("#").special).toBe(true);
  });

  it("treats other numeric characters as special, like .NET's char.IsLetterOrDigit", () => {
    expect(passwordChecks("²").special).toBe(true);
    expect(passwordChecks("²").number).toBe(false);
  });
});

describe("meetsPasswordRules", () => {
  it("needs every rule", () => {
    expect(meetsPasswordRules("parola12!")).toBe(true);
    expect(meetsPasswordRules("parola12")).toBe(false);
    expect(meetsPasswordRules("parolaaa!")).toBe(false);
    expect(meetsPasswordRules("pa1!")).toBe(false);
  });
});

describe("confirmationState", () => {
  it("is empty until the confirmation is typed, then match or mismatch", () => {
    expect(confirmationState("parola12!", "")).toBe("empty");
    expect(confirmationState("parola12!", "parola")).toBe("mismatch");
    expect(confirmationState("parola12!", "parola12!")).toBe("match");
    expect(confirmationState("", "x")).toBe("mismatch");
  });
});
