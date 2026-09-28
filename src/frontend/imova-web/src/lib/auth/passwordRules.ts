// The password policy, shown as a live checklist wherever a user chooses a password (register,
// reset, account settings). Mirrors the backend's PasswordRules and Identity options with the same
// definitions as .NET: length in UTF-16 units (string.Length), a "number" is a decimal digit
// (char.IsDigit), a "special character" anything that's neither a letter nor a decimal digit
// (!char.IsLetterOrDigit — a space, "²" or "½" count).

export const PASSWORD_MIN_LENGTH = 8;

export const PASSWORD_RULES = ["minLength", "number", "special"] as const;
export type PasswordRule = (typeof PASSWORD_RULES)[number];

export function passwordChecks(password: string): Record<PasswordRule, boolean> {
  return {
    minLength: password.length >= PASSWORD_MIN_LENGTH,
    number: /\p{Nd}/u.test(password),
    special: /[^\p{L}\p{Nd}]/u.test(password),
  };
}

export function meetsPasswordRules(password: string): boolean {
  const checks = passwordChecks(password);
  return PASSWORD_RULES.every((rule) => checks[rule]);
}

// "empty" until the user starts typing the confirmation — then it's always one of the other two.
export type ConfirmationState = "empty" | "match" | "mismatch";

export function confirmationState(password: string, confirmation: string): ConfirmationState {
  if (confirmation === "") return "empty";
  return confirmation === password ? "match" : "mismatch";
}
