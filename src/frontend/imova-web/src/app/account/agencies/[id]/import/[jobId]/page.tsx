import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { LinkButton } from "@/components/ui/Button";
import { ImportProgress } from "@/components/agency/import/ImportProgress";
import { ImportStatusBadge } from "@/components/agency/import/ImportStatusBadge";
import { getAgencyForMember, getImportJob, getImportProblemRows } from "@/lib/api/agencies";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { canManage } from "@/lib/agency/manage";
import { describeImportProblem, importPageHref, isImportRunning, sortProblems, type ImportTranslate } from "@/lib/agency/importJobs";
import { formatDateTime } from "@/lib/utils/format";
import { cn } from "@/lib/utils/cn";

// How many problem rows the page lists; the report has them all.
const SHOWN_ROWS = 200;

export async function generateMetadata({ params }: { params: Promise<{ id: string }> }): Promise<Metadata> {
  const { id } = await params;
  const [agency, t] = await Promise.all([getAgencyForMember(id), getTranslations("AgencyImport.page")]);
  return { title: agency ? t("jobPageTitle", { name: agency.name }) : undefined, robots: { index: false } };
}

// One import: its progress while it runs (polled), then the counts, what wasn't in the file, and
// every row with a problem — in the reader's language — with the report to download.
export default async function AgencyImportJobPage({ params }: { params: Promise<{ id: string; jobId: string }> }) {
  const { id, jobId } = await params;
  const [agency, profile, job, t, errors, locale] = await Promise.all([
    getAgencyForMember(id),
    getCurrentUserProfile(),
    getImportJob(id, jobId),
    getTranslations("AgencyImport.page"),
    getTranslations("Errors"),
    getLocale(),
  ]);
  if (!agency || !profile || !job || !canManage(agency.myRole, profile.roles.includes("Admin"))) notFound();

  const running = isImportRunning(job);
  const rows = running ? null : await getImportProblemRows(id, jobId, SHOWN_ROWS);
  const translate: ImportTranslate = (key, values) => (errors.has(key) ? errors(key, values) : null);

  return (
    <div className="space-y-6">
      <div>
        <Link href={importPageHref(agency.id)} className="text-sm font-medium text-ink-500 hover:text-ink-900">
          <span aria-hidden>←</span> {t("title")}
        </Link>
        <div className="mt-3 flex flex-wrap items-center gap-3">
          <h2 className="min-w-0 break-all font-display text-xl font-medium text-ink-950">{job.fileName}</h2>
          <ImportStatusBadge status={job.status} label={t(`status.${job.status}`)} />
        </div>
        <p className="mt-1 text-sm text-ink-500">
          {formatDateTime(job.createdAt, locale)}
          {job.uploadedByName && <> · {t("uploadedBy", { name: job.uploadedByName })}</>}
        </p>
      </div>

      {running && (
        <section className="rounded-2xl border border-line bg-white p-4 sm:p-5">
          <ImportProgress agencyId={agency.id} initial={job} />
        </section>
      )}

      {job.status === "Failed" && (
        <p role="alert" className="rounded-xl bg-accent-100 px-4 py-3 text-sm text-accent-700">
          {t("failedWhole")} {job.failureCode && (translate(`codes.${job.failureCode}`, { max: 2000, maxMb: 5, columns: "" }) ?? "")}
        </p>
      )}

      {job.status === "Completed" && (
        <>
          <dl className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            {(
              [
                ["created", job.createdCount],
                ["updated", job.updatedCount],
                ["failed", job.failedCount],
                ["warnings", job.warningCount],
              ] as const
            ).map(([key, value]) => (
              <div key={key} className="rounded-2xl border border-line bg-white p-4">
                <dt className="text-sm text-ink-600">{t(key)}</dt>
                <dd className={cn("mt-1 font-display text-2xl font-medium tabular-nums", key === "failed" && value > 0 ? "text-accent-700" : "text-ink-950")}>
                  {value}
                </dd>
              </div>
            ))}
          </dl>
          {job.notInFileCount > 0 && (
            <p className="rounded-xl bg-brand-50 px-4 py-3 text-sm text-brand-800">{t("notInFile", { count: job.notInFileCount })}</p>
          )}
        </>
      )}

      {rows && (
        <section aria-labelledby="problems-title" className="space-y-3">
          <div className="flex flex-wrap items-baseline justify-between gap-3">
            <h3 id="problems-title" className="text-base font-semibold text-ink-950">{t("problemsTitle")}</h3>
            {rows.totalCount > 0 && (
              <a href={`${importPageHref(agency.id)}/${job.id}/report`} className="text-sm font-medium text-brand-700 hover:underline">
                {t("report")}
              </a>
            )}
          </div>
          {rows.totalCount === 0 ? (
            <p className="text-sm text-ink-500">{t("noProblems")}</p>
          ) : (
            <>
              <p className="text-sm text-ink-600">{t("problemsIntro")}</p>
              <ul className="space-y-2">
                {rows.items.map((row) => (
                  <li key={row.rowNumber} className="rounded-2xl border border-line bg-white p-4">
                    <p className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm">
                      <span className="font-semibold text-ink-950">{t("row", { number: row.rowNumber })}</span>
                      {row.externalRef && <code className="rounded bg-ink-50 px-1.5 py-0.5 text-xs text-ink-700">{row.externalRef}</code>}
                      <span className={cn("text-xs font-medium", row.outcome === "Failed" ? "text-accent-700" : "text-emerald-700")}>
                        {t(`outcome.${row.outcome}`)}
                      </span>
                      {row.listingId && (
                        <Link href={`/property/${row.listingId}`} className="text-xs font-medium text-brand-700 hover:underline">
                          {t("viewListing")}
                        </Link>
                      )}
                    </p>
                    <ul className="mt-2 space-y-1.5">
                      {sortProblems(row.problems).map((problem, index) => (
                        <li key={index} className="flex flex-wrap items-start gap-x-2 gap-y-1 text-sm">
                          <span
                            className={cn(
                              "rounded-full px-2 py-0.5 text-xs font-medium",
                              problem.severity === "Error" ? "bg-red-50 text-red-700" : "bg-amber-50 text-amber-800",
                            )}
                          >
                            {t(`severity.${problem.severity}`)}
                          </span>
                          {problem.column && <code className="rounded bg-ink-50 px-1.5 py-0.5 text-xs text-ink-700">{problem.column}</code>}
                          <span className="min-w-0 flex-1 basis-48 break-words text-ink-800">{describeImportProblem(problem, translate)}</span>
                        </li>
                      ))}
                    </ul>
                  </li>
                ))}
              </ul>
              {rows.totalCount > rows.items.length && (
                <p className="text-sm text-ink-500">{t("moreRows", { shown: rows.items.length, total: rows.totalCount })}</p>
              )}
              <p className="text-xs text-ink-500">{t("reportNote")}</p>
            </>
          )}
        </section>
      )}

      {!running && (
        <div className="flex flex-wrap items-center gap-3">
          <LinkButton href={importPageHref(agency.id)} variant="secondary" size="sm">
            {t("another")}
          </LinkButton>
          {job.fileAvailable && <span className="text-xs text-ink-500">{t("fileKept")}</span>}
        </div>
      )}
    </div>
  );
}
