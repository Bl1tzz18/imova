import { describe, expect, it } from "vitest";
import { describeProblem, fieldKey, type Translate } from "./problem";

// A tiny stand-in for the "Errors" messages: key → template with {placeholders}.
const MESSAGES: Record<string, string> = {
  "codes.auth.invalidCredentials": "Email sau parolă greșită.",
  "codes.identity.DuplicateEmail": "Există deja un cont cu acest email.",
  "codes.password.noSpecial": "Parola trebuie să conțină un caracter special.",
  "codes.message.tooLong": "Mesajul poate avea cel mult {max} de caractere.",
  "codes.messaging.tooManyConversations": "Poți începe cel mult {max} conversații noi pe oră.",
  "fields.email": "Email",
  "fields.title": "Titlu",
  "validation.required": "Câmpul „{field}” este obligatoriu.",
  "validation.tooLong": "Câmpul „{field}” poate avea cel mult {max} caractere.",
  "validation.invalid": "Câmpul „{field}” nu este valid.",
  "validation.generic": "Unele date nu sunt valide.",
  "status.401": "Trebuie să te autentifici.",
  "status.default": "A apărut o eroare.",
};

const translate: Translate = (key, values) => {
  const template = MESSAGES[key];
  if (template === undefined) return null;
  return template.replace(/\{(\w+)\}/g, (_, name: string) => String(values?.[name] ?? `{${name}}`));
};

describe("describeProblem", () => {
  it("translates a problem's code, filling in its params", () => {
    expect(describeProblem({ status: 401, code: "auth.invalidCredentials" }, translate)).toBe("Email sau parolă greșită.");
    expect(
      describeProblem({ status: 429, code: "messaging.tooManyConversations", params: { max: 10 } }, translate),
    ).toBe("Poți începe cel mult 10 conversații noi pe oră.");
  });

  it("translates each validation error once, joined", () => {
    const message = describeProblem(
      {
        status: 400,
        errorCodes: [
          { field: "Email", code: "identity.DuplicateEmail" },
          { field: "Password", code: "password.noSpecial" },
          { field: "Password", code: "password.noSpecial" },
        ],
      },
      translate,
    );
    expect(message).toBe("Există deja un cont cu acest email. Parola trebuie să conțină un caracter special.");
  });

  it("falls back to a per-field message for built-in rules, with the field's translated name", () => {
    expect(describeProblem({ status: 400, errorCodes: [{ field: "Title", code: "NotEmptyValidator" }] }, translate)).toBe(
      "Câmpul „Titlu” este obligatoriu.",
    );
    expect(
      describeProblem({ status: 400, errorCodes: [{ field: "Title", code: "MaximumLengthValidator", params: { max: 200 } }] }, translate),
    ).toBe("Câmpul „Titlu” poate avea cel mult 200 caractere.");
    expect(describeProblem({ status: 400, errorCodes: [{ field: "Title", code: "PredicateValidator" }] }, translate)).toBe(
      "Câmpul „Titlu” nu este valid.",
    );
  });

  it("uses a generic message when neither the code nor the field is known", () => {
    expect(
      describeProblem({ status: 400, errorCodes: [{ field: "TypeSpecificAttributes.soilQualityScore", code: "PredicateValidator" }] }, translate),
    ).toBe("Unele date nu sunt valide.");
  });

  it("falls back to the status, then to a default — never to the API's English text", () => {
    expect(describeProblem({ status: 401 }, translate)).toBe("Trebuie să te autentifici.");
    expect(describeProblem({ status: 502, code: "somethingNew" }, translate)).toBe("A apărut o eroare.");
  });
});

describe("fieldKey", () => {
  it("uses the last path segment, first letter lowercased", () => {
    expect(fieldKey("NewPassword")).toBe("newPassword");
    expect(fieldKey("Contact.Name")).toBe("name");
    expect(fieldKey("TypeSpecificAttributes.rooms")).toBe("rooms");
  });
});
