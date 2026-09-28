import { getTranslations } from "next-intl/server";
import { describeProblem, readProblem } from "@/lib/api/problem";

// The user-facing text for a failed API response, in the current UI language. Server-side only
// (server actions / components). See lib/api/problem.ts for how codes become messages.
export async function apiErrorMessage(res: Response): Promise<string> {
  const [problem, t] = await Promise.all([readProblem(res), getTranslations("Errors")]);
  return describeProblem(problem, (key, values) => (t.has(key) ? t(key, values) : null));
}
