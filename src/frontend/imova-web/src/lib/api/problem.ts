// Turning an API error into text in the user's language. The API answers errors with a
// ProblemDetails body carrying language-neutral codes next to its English text (backend
// ErrorCodes / ProblemCodes):
//   { status, code?, params?, errorCodes?: [{ field, code, params }] }
// Only the codes are used here — the English `detail`/`errors` never reach the user. Messages live
// in messages/*.json under "Errors": codes.<code> for specific errors, validation.* + fields.* for
// generic per-field ones, status.<status> as the last resort.

export type ProblemErrorCode = { field?: string | null; code?: string | null; params?: Record<string, unknown> | null };

export type ApiProblem = {
  status: number;
  code?: string | null;
  params?: Record<string, unknown> | null;
  errorCodes?: ProblemErrorCode[] | null;
};

// Returns the translation for `key` (a path under "Errors"), or null when there's none.
export type Translate = (key: string, values?: Record<string, string | number>) => string | null;

export async function readProblem(res: Response): Promise<ApiProblem> {
  const body = (await res.json().catch(() => null)) as Partial<ApiProblem> | null;
  return { ...(body ?? {}), status: res.status };
}

// FluentValidation's built-in rule codes (for rules without a specific ErrorCodes code).
const GENERIC_VALIDATION: Record<string, string> = {
  NotEmptyValidator: "required",
  NotNullValidator: "required",
  MaximumLengthValidator: "tooLong",
  MinimumLengthValidator: "tooShort",
  EmailValidator: "email",
};

export function describeProblem(problem: ApiProblem, translate: Translate): string {
  const fromCodes = (problem.errorCodes ?? []).map((error) => describeErrorCode(error, translate));
  const unique = [...new Set(fromCodes.filter((m): m is string => !!m))];
  if (unique.length > 0) return unique.join(" ");

  const specific = problem.code ? translate(`codes.${problem.code}`, toValues(problem.params)) : null;
  return specific ?? translate(`status.${problem.status}`) ?? translate("status.default") ?? "";
}

function describeErrorCode(error: ProblemErrorCode, translate: Translate): string | null {
  const values = toValues(error.params);
  if (error.code) {
    const specific = translate(`codes.${error.code}`, values);
    if (specific) return specific;
  }

  const kind = (error.code && GENERIC_VALIDATION[error.code]) || "invalid";
  const field = error.field ? translate(`fields.${fieldKey(error.field)}`) : null;
  return field
    ? translate(`validation.${kind}`, { ...values, field })
    : translate("validation.generic");
}

// "Contact.Name" → "name", "NewPassword" → "newPassword", "TypeSpecificAttributes.rooms" → "rooms".
export function fieldKey(field: string): string {
  const last = field.split(".").at(-1) ?? field;
  return last.charAt(0).toLowerCase() + last.slice(1);
}

function toValues(params: Record<string, unknown> | null | undefined): Record<string, string | number> {
  const values: Record<string, string | number> = {};
  for (const [key, value] of Object.entries(params ?? {})) {
    if (typeof value === "number" || typeof value === "string") values[key] = value;
  }
  return values;
}
