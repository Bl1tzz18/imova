import type { ListingImportJob, ListingImportProblem } from "@/types/agency";

// The CSV import pages, site admins only (/admin/agencies/[id]/import[/jobId]) — IMOVA imports agencies'
// listings by hand. Pure rules, Vitest-covered.

export const IMPORT_MAX_BYTES = 5 * 1024 * 1024;

// How often the result page asks how far an import has got.
export const IMPORT_POLL_MS = 3000;

export function importPageHref(agencyId: string): string {
  return `/admin/agencies/${agencyId}/import`;
}

export function importJobHref(agencyId: string, jobId: string): string {
  return `${importPageHref(agencyId)}/${jobId}`;
}

// Waiting for the Worker or running — the page keeps polling, and no second upload is possible.
export function isImportRunning(job: Pick<ListingImportJob, "status">): boolean {
  return job.status === "Pending" || job.status === "Processing";
}

// 0–100: rows done of the file's rows (unknown until the Worker has read the file).
export function importProgress(job: Pick<ListingImportJob, "status" | "totalRows" | "processedRows">): number {
  if (job.status === "Completed" || job.status === "Failed") return 100;
  if (job.totalRows <= 0) return 0;
  return Math.min(100, Math.floor((job.processedRows / job.totalRows) * 100));
}

// The file can only be chosen when it's a CSV-ish name and within the limit; the API checks again.
export function importFileProblem(file: { name: string; size: number }): "notCsv" | "tooLarge" | "empty" | null {
  if (!/\.(csv|txt)$/i.test(file.name)) return "notCsv";
  if (file.size === 0) return "empty";
  if (file.size > IMPORT_MAX_BYTES) return "tooLarge";
  return null;
}

// Returns the translation for `key` (a path under "Errors"), or null when there's none.
export type ImportTranslate = (key: string, values?: Record<string, string | number>) => string | null;

// FluentValidation's own rule codes (the listing form's rules without a specific code) → a generic
// message about the column.
const GENERIC: Record<string, string> = {
  NotEmptyValidator: "required",
  NotNullValidator: "required",
  MaximumLengthValidator: "tooLong",
  InclusiveBetweenValidator: "between",
  GreaterThanValidator: "greaterThan",
  GreaterThanOrEqualValidator: "greaterThan",
  LessThanValidator: "lessThan",
  LessThanOrEqualValidator: "lessThan",
};

// One problem of a row, in the reader's language: its own message (Errors.codes.<code>) — for a
// photo or coordinates warning with its reason translated too — else a generic one about the column.
export function describeImportProblem(problem: ListingImportProblem, translate: ImportTranslate): string {
  const values: Record<string, string | number> = { ...(problem.params ?? {}), column: problem.column ?? "" };

  if (problem.code === "import.photoFailed" && problem.params?.reason) {
    values.reason = translate(`codes.import.photoReason.${problem.params.reason}`) ?? problem.params.reason;
  }

  if (problem.code === "import.coordinatesIgnored" && problem.params?.reason) {
    values.reason = translate(`codes.import.coordinatesReason.${problem.params.reason}`, values) ?? problem.params.reason;
  }

  const specific = translate(`codes.${problem.code}`, values);
  if (specific) return specific;

  const kind = GENERIC[problem.code] ?? "invalid";
  return translate(`codes.import.generic.${kind}`, values) ?? translate("codes.import.generic.invalid", values) ?? problem.message;
}

// Errors first, then warnings — the order the result page lists a row's problems in.
export function sortProblems(problems: readonly ListingImportProblem[]): ListingImportProblem[] {
  return [...problems].sort((a, b) => (a.severity === b.severity ? 0 : a.severity === "Error" ? -1 : 1));
}
