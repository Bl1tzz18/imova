import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { ImportStatusBadge } from "@/components/agency/import/ImportStatusBadge";
import { ImportUploadForm } from "@/components/agency/import/ImportUploadForm";
import { getAgencyForMember, getImportJobs } from "@/lib/api/agencies";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { agencyManagePath, canManage } from "@/lib/agency/manage";
import { importJobHref, isImportRunning } from "@/lib/agency/importJobs";
import { IMPORT_PROPERTY_TYPES } from "@/lib/agency/importReadme";
import { formatDateTime } from "@/lib/utils/format";

export async function generateMetadata({ params }: { params: Promise<{ id: string }> }): Promise<Metadata> {
  const { id } = await params;
  const [agency, t] = await Promise.all([getAgencyForMember(id), getTranslations("AgencyImport.page")]);
  return { title: agency ? t("pageTitle", { name: agency.name }) : undefined, robots: { index: false } };
}

// The agency's CSV import: the templates and guides to prepare a file, the upload, and the past imports.
// Its Owners and Admins, and site admins (who onboard agencies). Uploading needs the agency verified
// and active; preparing a file doesn't.
export default async function AgencyImportPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const [agency, profile, jobs, t, types, locale] = await Promise.all([
    getAgencyForMember(id),
    getCurrentUserProfile(),
    getImportJobs(id),
    getTranslations("AgencyImport.page"),
    getTranslations("PropertyType"),
    getLocale(),
  ]);
  if (!agency || !profile || !jobs || !canManage(agency.myRole, profile.roles.includes("Admin"))) notFound();

  const running = jobs.find(isImportRunning);
  const base = `/account/agencies/${agency.id}/import`;

  return (
    <div className="space-y-6">
      <div>
        <Link href={agencyManagePath(agency.id, "listings")} className="text-sm font-medium text-ink-500 hover:text-ink-900">
          <span aria-hidden>←</span> {t("back")}
        </Link>
        <h2 className="mt-3 font-display text-xl font-medium text-ink-950">{t("title")}</h2>
        <p className="mt-1 max-w-2xl text-sm text-ink-600">{t("intro")}</p>
      </div>

      {!agency.isVerified && <p className="rounded-xl bg-accent-100 px-4 py-3 text-sm text-accent-700">{t("notVerified")}</p>}
      {agency.status === "Deactivated" && <p className="rounded-xl bg-accent-100 px-4 py-3 text-sm text-accent-700">{t("inactive")}</p>}

      <section aria-labelledby="prepare-title" className="rounded-2xl border border-line bg-white p-4 sm:p-5">
        <h3 id="prepare-title" className="text-base font-semibold text-ink-950">{t("prepareTitle")}</h3>
        <p className="mt-1 text-sm text-ink-600">{t("prepareIntro")}</p>
        <ul className="mt-3 divide-y divide-line">
          {IMPORT_PROPERTY_TYPES.map((type) => (
            <li key={type} className="flex flex-wrap items-center justify-between gap-x-4 gap-y-1 py-2.5">
              <span className="text-sm font-medium text-ink-900">{types(type)}</span>
              <span className="flex gap-4 text-sm">
                <a href={`${base}/template?propertyType=${type}`} className="py-1 font-medium text-brand-700 hover:underline">
                  {t("template")}
                </a>
                <a href={`${base}/readme?propertyType=${type}`} className="py-1 font-medium text-brand-700 hover:underline">
                  {t("guide")}
                </a>
              </span>
            </li>
          ))}
        </ul>
        <a href={`${base}/readme`} className="mt-2 inline-block py-1 text-sm font-medium text-brand-700 hover:underline">
          {t("guideAll")}
        </a>
      </section>

      <section aria-labelledby="upload-title" className="rounded-2xl border border-line bg-white p-4 sm:p-5">
        <h3 id="upload-title" className="text-base font-semibold text-ink-950">{t("uploadTitle")}</h3>
        <p className="mt-1 mb-4 text-sm text-ink-600">{t("uploadHint")}</p>
        {running && (
          <p className="mb-4 rounded-xl bg-brand-50 px-4 py-3 text-sm text-brand-800">
            {t("running")}{" "}
            <Link href={importJobHref(agency.id, running.id)} className="font-medium underline">
              {t("viewProgress")}
            </Link>
          </p>
        )}
        <ImportUploadForm agencyId={agency.id} disabled={!agency.isVerified || agency.status === "Deactivated" || !!running} />
      </section>

      <section aria-labelledby="history-title">
        <h3 id="history-title" className="text-base font-semibold text-ink-950">{t("historyTitle")}</h3>
        {jobs.length === 0 ? (
          <p className="mt-2 text-sm text-ink-500">{t("historyEmpty")}</p>
        ) : (
          <ul className="mt-3 divide-y divide-line rounded-2xl border border-line bg-white">
            {jobs.map((job) => (
              <li key={job.id} className="flex flex-wrap items-center gap-x-4 gap-y-1 px-4 py-3">
                <div className="min-w-0 flex-1 basis-56">
                  <Link href={importJobHref(agency.id, job.id)} className="block truncate text-sm font-medium text-ink-900 hover:underline">
                    {job.fileName}
                  </Link>
                  <p className="text-xs text-ink-500">
                    {formatDateTime(job.createdAt, locale)}
                    {job.uploadedByName && <> · {t("uploadedBy", { name: job.uploadedByName })}</>}
                  </p>
                </div>
                {job.status === "Completed" && (
                  <span className="text-sm text-ink-600 tabular-nums">
                    {t("counts", { created: job.createdCount, updated: job.updatedCount, failed: job.failedCount })}
                  </span>
                )}
                <ImportStatusBadge status={job.status} label={t(`status.${job.status}`)} />
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
