"use client";

import { useRef, useState, useTransition } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { TextAreaInput } from "@/components/ui/Field";
import { REPORT_REASONS, REPORT_TEXT_MAX, reportFormProblem } from "@/lib/listing/reports";
import { reportListing } from "@/lib/listing/reportActions";
import { cn } from "@/lib/utils/cn";
import type { ListingReportReason } from "@/types/listingReport";

const flagIcon = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" className="h-4 w-4" aria-hidden>
    <path d="M5 21V4m0 0h11l-2 4 2 4H5" strokeLinecap="round" strokeLinejoin="round" />
  </svg>
);

const quietLink = "mt-4 inline-flex items-center gap-1.5 text-xs font-medium text-ink-500 transition-colors hover:text-red-600";

// "Raportează anunțul", under the contact card — deliberately quiet (it's for the few who need it).
// Signed out, it's a link to sign in and come back. The form is a native <dialog>: focus stays
// inside, Escape closes it. The owner never learns who reported (the dialog says so).
export function ReportListingButton({ listingId, signedIn }: { listingId: string; signedIn: boolean }) {
  const t = useTranslations("ListingReport");
  const dialog = useRef<HTMLDialogElement>(null);
  const [reason, setReason] = useState<ListingReportReason | null>(null);
  const [details, setDetails] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState<"new" | "amended" | null>(null);
  const [signedOut, setSignedOut] = useState(false);
  const [pending, startTransition] = useTransition();

  if (!signedIn) {
    return (
      <Link href={`/login?next=${encodeURIComponent(`/property/${listingId}`)}`} className={quietLink}>
        {flagIcon}
        {t("open")}
      </Link>
    );
  }

  function open() {
    setError(null);
    setSent(null);
    dialog.current?.showModal();
  }

  function close() {
    dialog.current?.close();
    if (sent) {
      // A later report starts from a clean form.
      setReason(null);
      setDetails("");
    }
  }

  function submit() {
    const problem = reportFormProblem(reason, details);
    if (problem) {
      setError(t(`problem.${problem}`));
      return;
    }

    setError(null);
    startTransition(async () => {
      const result = await reportListing(listingId, reason!, details);
      if (result.error !== undefined) {
        if (result.signedOut) setSignedOut(true);
        else setError(result.error);
        return;
      }
      setSent(result.data.amended ? "amended" : "new");
    });
  }

  const detailsRequired = reason === "Other";

  return (
    <>
      <button type="button" onClick={open} className={quietLink}>
        {flagIcon}
        {t("open")}
      </button>

      <dialog
        ref={dialog}
        aria-labelledby="report-listing-title"
        onClick={(e) => e.target === dialog.current && close()}
        className="m-auto w-[calc(100%-2rem)] max-w-md rounded-2xl bg-white p-0 text-left shadow-xl backdrop:bg-ink-950/50"
      >
        <div className="p-6">
          {sent ? (
            <div role="status" className="flex flex-col items-center gap-3 py-4 text-center">
              <span className="flex h-12 w-12 items-center justify-center rounded-full bg-brand-50 text-brand-700">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-6 w-6" aria-hidden>
                  <path d="m5 12.5 4.5 4.5L19 7.5" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
              </span>
              <h2 id="report-listing-title" className="font-display text-lg font-medium text-ink-950">
                {sent === "amended" ? t("amendedTitle") : t("sentTitle")}
              </h2>
              <p className="text-sm text-ink-600">{t("sentBody")}</p>
              {reason === "Fraud" && (
                <p className="rounded-xl bg-accent-100/60 px-3.5 py-2.5 text-xs text-accent-700">{t("fraudTip")}</p>
              )}
              <Button type="button" variant="secondary" size="sm" className="mt-2" onClick={close}>
                {t("close")}
              </Button>
            </div>
          ) : signedOut ? (
            <div className="flex flex-col gap-3 py-2">
              <h2 id="report-listing-title" className="font-display text-lg font-medium text-ink-950">
                {t("title")}
              </h2>
              <p className="text-sm text-ink-600">{t("signedOut")}</p>
              <div className="flex justify-end gap-2">
                <Button type="button" variant="ghost" size="sm" onClick={close}>
                  {t("cancel")}
                </Button>
                <Link
                  href={`/login?next=${encodeURIComponent(`/property/${listingId}`)}`}
                  className="inline-flex h-9 items-center rounded-full bg-accent-500 px-3.5 text-sm font-medium text-white hover:bg-accent-600"
                >
                  {t("signIn")}
                </Link>
              </div>
            </div>
          ) : (
            <form
              onSubmit={(e) => {
                e.preventDefault();
                submit();
              }}
            >
              <h2 id="report-listing-title" className="font-display text-lg font-medium text-ink-950">
                {t("title")}
              </h2>
              <p className="mt-1 text-sm text-ink-500">{t("intro")}</p>

              <fieldset className="mt-4">
                <legend className="text-sm font-medium text-ink-700">{t("reasonLabel")}</legend>
                <div className="mt-2 flex flex-col gap-2">
                  {REPORT_REASONS.map((value) => (
                    <label
                      key={value}
                      className={cn(
                        "flex cursor-pointer gap-3 rounded-xl border px-3.5 py-2.5 transition-colors",
                        reason === value ? "border-brand-500 bg-brand-50/60" : "border-ink-200 hover:border-ink-300",
                      )}
                    >
                      <input
                        type="radio"
                        name="reason"
                        value={value}
                        checked={reason === value}
                        onChange={() => {
                          setReason(value);
                          setError(null);
                        }}
                        className="mt-1 accent-brand-600"
                      />
                      <span>
                        <span className="block text-sm font-medium text-ink-900">{t(`reason.${value}`)}</span>
                        <span className="block text-xs text-ink-500">{t(`reasonHint.${value}`)}</span>
                      </span>
                    </label>
                  ))}
                </div>
              </fieldset>

              <div className="mt-4">
                <div className="flex items-baseline justify-between">
                  <label htmlFor="report-details" className="text-sm font-medium text-ink-700">
                    {detailsRequired ? t("detailsRequired") : t("detailsOptional")}
                  </label>
                  <span
                    id="report-details-count"
                    className={cn("text-xs", details.length > REPORT_TEXT_MAX ? "text-red-600" : "text-ink-400")}
                  >
                    {details.length}/{REPORT_TEXT_MAX}
                  </span>
                </div>
                <TextAreaInput
                  id="report-details"
                  aria-describedby="report-details-count"
                  value={details}
                  onChange={(e) => setDetails(e.target.value)}
                  rows={3}
                  placeholder={t("detailsPlaceholder")}
                  className="mt-1.5"
                />
              </div>

              {error && (
                <p role="alert" className="mt-3 text-sm text-red-600">
                  {error}
                </p>
              )}

              <p className="mt-3 text-xs text-ink-400">{t("privacy")}</p>

              <div className="mt-4 flex justify-end gap-2">
                <Button type="button" variant="ghost" size="sm" onClick={close}>
                  {t("cancel")}
                </Button>
                <Button type="submit" size="sm" disabled={pending || !reason}>
                  {pending ? t("sending") : t("send")}
                </Button>
              </div>
            </form>
          )}
        </div>
      </dialog>
    </>
  );
}
