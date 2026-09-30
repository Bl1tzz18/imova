import type { ListingReportReason, ListingReporter } from "@/types/listingReport";

// In the order the report form lists them: the most serious first, "Other" last.
export const REPORT_REASONS: readonly ListingReportReason[] = [
  "Fraud",
  "WrongInformation",
  "NoLongerAvailable",
  "Duplicate",
  "Other",
];

// Same limit as ListingReport.MaxDetailsLength / MaxNoteLength on the API.
export const REPORT_TEXT_MAX = 1000;

export type ReportFormProblem = "reasonRequired" | "detailsRequired" | "detailsTooLong";

// The report form's own check, before anything is sent — the same rules the API applies.
export function reportFormProblem(reason: ListingReportReason | null, details: string): ReportFormProblem | null {
  if (!reason) return "reasonRequired";
  if (details.length > REPORT_TEXT_MAX) return "detailsTooLong";
  if (reason === "Other" && !details.trim()) return "detailsRequired";
  return null;
}

// A hint for the admin next to a reporter's name, from how their earlier reports went:
//  - "deleted": the account is gone (the report stays);
//  - "first": this is the first listing they've reported;
//  - "oftenDismissed": at least 3 dismissed, and half or more of all they filed — worth a second
//    look (a competitor, or someone reporting in bulk);
//  - null: nothing notable.
export type ReporterSignal = "deleted" | "first" | "oftenDismissed" | null;

export function reporterSignal(reporter: ListingReporter): ReporterSignal {
  if (reporter.isDeleted) return "deleted";
  if (reporter.reportsFiled <= 1) return "first";
  if (reporter.reportsDismissed >= 3 && reporter.reportsDismissed * 2 >= reporter.reportsFiled) return "oftenDismissed";
  return null;
}

// How long a case has been waiting, in the largest whole unit ("3 days", "5 hours", "12 minutes")
// — for Intl.RelativeTimeFormat. Anything under a minute (or a clock slightly behind) is 0 minutes.
export function waitingFor(sinceIso: string, now: Date): { value: number; unit: "minute" | "hour" | "day" } {
  const minutes = Math.max(0, Math.floor((now.getTime() - new Date(sinceIso).getTime()) / 60_000));
  if (minutes >= 60 * 24) return { value: Math.floor(minutes / (60 * 24)), unit: "day" };
  if (minutes >= 60) return { value: Math.floor(minutes / 60), unit: "hour" };
  return { value: minutes, unit: "minute" };
}

// The most frequent reason of a case (the API sends them most frequent first) — it picks the
// suspension text the admin starts from.
export function topReason(reasons: { reason: ListingReportReason; count: number }[]): ListingReportReason | null {
  return reasons[0]?.reason ?? null;
}
