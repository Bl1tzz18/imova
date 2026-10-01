"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { TextAreaInput } from "@/components/ui/Field";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { ListingStats } from "@/components/property/ListingStats";
import { dismissListingReports, suspendListing } from "@/lib/admin/actions";
import { moderationHref, type ReportView } from "@/lib/admin/moderationTabs";
import { REPORT_TEXT_MAX, reporterSignal, topReason, waitingFor } from "@/lib/listing/reports";
import { coverPhoto } from "@/lib/listing/view";
import { cn } from "@/lib/utils/cn";
import { formatDateTime, formatLocation, formatPrice } from "@/lib/utils/format";
import type { ListingReportItem, ListingReportReason, ReportedListing } from "@/types/listingReport";

// Reasons coloured by how serious they are, so a fraud case stands out at a glance.
const REASON_TONE: Record<ListingReportReason, string> = {
  Fraud: "bg-red-50 text-red-700 ring-red-200",
  WrongInformation: "bg-amber-50 text-amber-800 ring-amber-200",
  NoLongerAvailable: "bg-ink-100 text-ink-700 ring-ink-200",
  Duplicate: "bg-ink-100 text-ink-700 ring-ink-200",
  Other: "bg-brand-50 text-brand-800 ring-brand-100",
};

// Reports shown per case before "show all".
const VISIBLE_REPORTS = 3;

// The reports tab of /admin/moderation: one card per reported listing. Each card answers, top to
// bottom: what listing is it (and whose), how bad does it look (how many people, which reasons,
// for how long), who said what (each report with its reporter's record), and what to do about it —
// suspend (the owner sees the reason) or dismiss (a note for the other admins). In the history
// (view "resolved") the card shows the decision instead of the actions.
// `now` comes from the server render, so "waiting for …" reads the same on the server and in the
// browser (no hydration mismatch).
export function ReportedListingsQueue({ cases, view, now }: { cases: ReportedListing[]; view: ReportView; now: string }) {
  return (
    <div className="flex flex-col gap-4">
      {cases.map((c) => (
        <ReportCase key={`${c.listing.id}:${c.resolution?.resolvedAt ?? "open"}`} reported={c} view={view} now={now} />
      ))}
    </div>
  );
}

function ReportCase({ reported, view, now }: { reported: ReportedListing; view: ReportView; now: string }) {
  const t = useTranslations("AdminListingReports");
  const tReason = useTranslations("ListingReport.reason");
  const locale = useLocale();
  const { listing, reports, reasons, resolution } = reported;
  const [showAll, setShowAll] = useState(false);
  const cover = coverPhoto(listing);
  const location = formatLocation(listing.property.location);
  const reporters = new Set(reports.map((r) => r.reporter.userId)).size;
  const waited = waitingFor(reported.firstReportedAt, new Date(now));
  const relative = new Intl.RelativeTimeFormat(locale, { numeric: "auto" });
  const visible = showAll ? reports : reports.slice(0, VISIBLE_REPORTS);
  const hasFraud = reasons.some((r) => r.reason === "Fraud");

  return (
    <article
      className={cn(
        "overflow-hidden rounded-2xl border bg-white",
        view === "open" && hasFraud ? "border-red-200" : "border-ink-100",
      )}
    >
      {/* The listing, and whose it is. */}
      <div className="flex flex-col gap-3 p-4 sm:flex-row sm:items-start sm:gap-4">
        <a
          href={`/property/${listing.id}`}
          target="_blank"
          rel="noreferrer"
          className="flex h-[76px] w-[100px] shrink-0 items-center justify-center overflow-hidden rounded-xl bg-gradient-to-br from-brand-800 to-brand-600"
        >
          {cover ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={cover.url} alt={listing.title} className="h-full w-full object-cover" />
          ) : (
            <PropertyIcon type={listing.property.propertyType} className="h-7 w-7 text-white/40" />
          )}
        </a>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <a
              href={`/property/${listing.id}`}
              target="_blank"
              rel="noreferrer"
              className="truncate text-sm font-semibold text-ink-900 hover:underline"
            >
              {listing.title}
            </a>
            <span
              className={cn(
                "rounded-full px-2 py-0.5 text-[11px] font-medium",
                listing.status === "Active" ? "bg-emerald-50 text-emerald-700" : "bg-ink-100 text-ink-600",
              )}
            >
              {t(`status.${listing.status}`)}
            </span>
          </div>
          <p className="mt-0.5 text-xs text-ink-500">
            {formatPrice(listing.price.amount, listing.price.currency)}
            {location && ` · ${location}`}
          </p>
          <ListingStats listing={listing} showNumber className="mt-0.5" />
          <p className="mt-1.5 text-xs text-ink-600">
            <span className="text-ink-400">{t("publishedBy")} </span>
            <span className="font-medium text-ink-800">{listing.publisher.displayName}</span>
          </p>
          {/* The listing's contact details (the API sends them, hidden phone included, to admins). */}
          {listing.contact && (listing.contact.phone || listing.contact.email) && (
            <p className="mt-0.5 text-xs text-ink-600">
              <span className="text-ink-400">{t("contact")} </span>
              {[listing.contact.personType === "Other" ? listing.contact.name : null, listing.contact.phone, listing.contact.email]
                .filter(Boolean)
                .join(" · ")}
            </p>
          )}
        </div>
      </div>

      {/* How bad it looks. */}
      <div className="border-t border-ink-100 bg-ink-50/60 px-4 py-3">
        <p className="text-sm text-ink-800">
          <span className="font-semibold">{t("reportCount", { count: reported.reportCount })}</span>
          {" · "}
          {t("reporterCount", { count: reporters })}
          {view === "open" && (
            <>
              {" · "}
              <span className={cn(waited.unit === "day" && waited.value >= 2 && "font-medium text-red-600")}>
                {t("waiting", { since: relative.format(-waited.value, waited.unit) })}
              </span>
            </>
          )}
        </p>
        <div className="mt-2 flex flex-wrap gap-1.5">
          {reasons.map((r) => (
            <span key={r.reason} className={cn("rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset", REASON_TONE[r.reason])}>
              {tReason(r.reason)}
              {r.count > 1 && ` ×${r.count}`}
            </span>
          ))}
        </div>
      </div>

      {/* Who said what. */}
      <ul className="divide-y divide-ink-100 border-t border-ink-100">
        {visible.map((report) => (
          <ReportRow key={report.id} report={report} />
        ))}
      </ul>
      {reports.length > VISIBLE_REPORTS && (
        <button
          type="button"
          onClick={() => setShowAll((v) => !v)}
          className="w-full border-t border-ink-100 px-4 py-2 text-left text-xs font-medium text-brand-700 hover:bg-ink-50"
        >
          {showAll ? t("showFewer") : t("showAll", { count: reports.length })}
        </button>
      )}

      {/* What to do — or what was decided. */}
      {resolution ? (
        <div
          className={cn(
            "border-t px-4 py-3 text-sm",
            resolution.outcome === "ListingSuspended" ? "border-red-100 bg-red-50/60 text-red-900" : "border-ink-100 bg-ink-50 text-ink-700",
          )}
        >
          <p className="font-medium">
            {t(`decided.${resolution.outcome}`, {
              name: resolution.resolvedByName ?? t("unknownAdmin"),
              date: formatDateTime(resolution.resolvedAt, locale),
            })}
          </p>
          {resolution.note && (
            <p className="mt-1 whitespace-pre-line text-xs">
              <span className="font-medium">{resolution.outcome === "ListingSuspended" ? t("reasonShownToOwner") : t("adminNote")}:</span>{" "}
              {resolution.note}
            </p>
          )}
          {resolution.outcome === "ListingSuspended" && listing.status === "Suspended" && (
            <Link href={moderationHref({ tab: "suspended" })} className="mt-1.5 inline-block text-xs font-medium text-brand-700 hover:underline">
              {t("reinstateHint")}
            </Link>
          )}
        </div>
      ) : (
        <CaseActions reported={reported} />
      )}
    </article>
  );
}

function ReportRow({ report }: { report: ListingReportItem }) {
  const t = useTranslations("AdminListingReports");
  const tReason = useTranslations("ListingReport.reason");
  const locale = useLocale();
  const { reporter } = report;
  const signal = reporterSignal(reporter);

  return (
    <li className="px-4 py-3">
      <div className="flex flex-wrap items-center gap-x-2 gap-y-1 text-xs">
        <span className={cn("rounded-full px-2 py-0.5 font-medium ring-1 ring-inset", REASON_TONE[report.reason])}>
          {tReason(report.reason)}
        </span>
        {reporter.isDeleted ? (
          <span className="italic text-ink-500">{t("deletedAccount")}</span>
        ) : (
          <span className="text-ink-800">
            <span className="font-medium">{reporter.displayName ?? t("noName")}</span>
            {reporter.email && <span className="text-ink-500"> · {reporter.email}</span>}
          </span>
        )}
        <span className="text-ink-400">· {formatDateTime(report.createdAt, locale)}</span>
        {report.updatedAt !== report.createdAt && <span className="text-ink-400">({t("edited")})</span>}
      </div>

      {report.details ? (
        <blockquote className="mt-2 whitespace-pre-line border-l-2 border-ink-200 pl-3 text-sm text-ink-800">{report.details}</blockquote>
      ) : (
        <p className="mt-1.5 text-xs italic text-ink-400">{t("noDetails")}</p>
      )}

      {!reporter.isDeleted && (
        <p className={cn("mt-1.5 text-[11px]", signal === "oftenDismissed" ? "font-medium text-amber-700" : "text-ink-400")}>
          {signal === "first"
            ? t("record.first")
            : t("record.history", { filed: reporter.reportsFiled, dismissed: reporter.reportsDismissed })}
          {signal === "oftenDismissed" && ` — ${t("record.oftenDismissed")}`}
        </p>
      )}
    </li>
  );
}

function CaseActions({ reported }: { reported: ReportedListing }) {
  const t = useTranslations("AdminListingReports");
  const { listing } = reported;
  const canSuspend = listing.status === "Active";
  const [panel, setPanel] = useState<"suspend" | "dismiss" | null>(null);
  const [text, setText] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function openPanel(next: "suspend" | "dismiss") {
    setError(null);
    setPanel(next);
    // Suspending starts from a ready-made reason for the most frequent complaint — the admin
    // edits it; the owner will read it.
    const top = topReason(reported.reasons);
    setText(next === "suspend" && top ? t(`suspendTemplate.${top}`) : "");
  }

  function confirm() {
    if (panel === "suspend" && !text.trim()) {
      setError(t("suspendReasonRequired"));
      return;
    }

    setError(null);
    startTransition(async () => {
      const result = panel === "suspend" ? await suspendListing(listing.id, text.trim()) : await dismissListingReports(listing.id, text);
      if (result.error) {
        setError(result.error);
        return;
      }
      setPanel(null);
    });
  }

  return (
    <div className="border-t border-ink-100 px-4 py-3">
      {!canSuspend && <p className="mb-2 text-xs text-ink-500">{t("notActive", { status: t(`status.${listing.status}`) })}</p>}

      {panel ? (
        <div className="flex flex-col gap-2">
          <label className="text-sm font-medium text-ink-800">
            {panel === "suspend" ? t("suspendLabel") : t("dismissLabel")}
            <TextAreaInput
              value={text}
              onChange={(e) => setText(e.target.value)}
              rows={3}
              maxLength={REPORT_TEXT_MAX}
              placeholder={panel === "dismiss" ? t("dismissPlaceholder") : undefined}
              className="mt-1.5 font-normal"
              autoFocus
            />
          </label>
          <p className="text-xs text-ink-500">{panel === "suspend" ? t("suspendHint") : t("dismissHint")}</p>
          <div className="flex flex-wrap gap-2">
            <Button
              size="sm"
              variant={panel === "suspend" ? "danger" : "primary"}
              disabled={pending}
              onClick={confirm}
            >
              {panel === "suspend" ? t("confirmSuspend") : t("confirmDismiss", { count: reported.reportCount })}
            </Button>
            <Button size="sm" variant="ghost" disabled={pending} onClick={() => setPanel(null)}>
              {t("cancel")}
            </Button>
          </div>
        </div>
      ) : (
        <div className="flex flex-wrap items-center gap-2">
          {canSuspend && (
            <Button size="sm" variant="danger" onClick={() => openPanel("suspend")}>
              {t("suspend")}
            </Button>
          )}
          <Button size="sm" variant="secondary" onClick={() => openPanel("dismiss")}>
            {canSuspend ? t("dismiss") : t("close")}
          </Button>
          <a
            href={`/property/${listing.id}`}
            target="_blank"
            rel="noreferrer"
            className="ml-auto text-xs font-medium text-brand-700 hover:underline"
          >
            {t("openListing")} ↗
          </a>
        </div>
      )}

      {error && (
        <p role="alert" className="mt-2 text-xs text-red-600">
          {error}
        </p>
      )}
    </div>
  );
}
