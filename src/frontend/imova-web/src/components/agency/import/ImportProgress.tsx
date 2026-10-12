"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { IMPORT_POLL_MS, importJobHref, importProgress, isImportRunning } from "@/lib/agency/importJobs";
import type { ListingImportJob } from "@/types/agency";

// While an import waits or runs: its progress bar, refreshed every few seconds; once it's done the
// page reloads with the results.
export function ImportProgress({ agencyId, initial }: { agencyId: string; initial: ListingImportJob }) {
  const t = useTranslations("AgencyImport.page");
  const router = useRouter();
  const [job, setJob] = useState(initial);

  useEffect(() => {
    if (!isImportRunning(job)) return;
    const timer = window.setTimeout(async () => {
      const res = await fetch(`${importJobHref(agencyId, job.id)}/status`, { cache: "no-store" }).catch(() => null);
      const next = res?.ok ? ((await res.json()) as ListingImportJob) : null;
      // A new status (Pending → Processing, or done) changes what the server renders: the badge, then the results.
      if (next && next.status !== job.status) {
        router.refresh();
      }

      setJob(next ?? { ...job });
    }, IMPORT_POLL_MS);
    return () => window.clearTimeout(timer);
  }, [agencyId, job, router]);

  const percent = importProgress(job);
  return (
    <div className="space-y-2" aria-live="polite">
      <div
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={percent}
        className="h-2.5 overflow-hidden rounded-full bg-ink-100"
      >
        <div className="h-full rounded-full bg-brand-500 transition-[width] duration-500" style={{ width: `${percent}%` }} />
      </div>
      <p className="text-sm text-ink-600 tabular-nums">
        {job.status === "Pending" || job.totalRows === 0 ? t("waiting") : t("progress", { done: job.processedRows, total: job.totalRows })}
      </p>
    </div>
  );
}
