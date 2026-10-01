"use client";

import { useEffect, useRef } from "react";
import { useTranslations } from "next-intl";
import { Button, LinkButton } from "@/components/ui/Button";
import { savedOutcome, type SavedOutcome } from "@/lib/listing/savedDialog";
import { formatPercentChange } from "@/lib/listing/priceHistory";
import { formatPrice } from "@/lib/utils/format";
import type { PriceReduction } from "@/types/listing";

// Opens after every successful save of the edit form (savedAt changes per save) and says what the
// save actually did — live, back in review, waiting for the email, or saved but not public (see
// savedOutcome) — then offers the next step: see the listing (first, and focused), go back to
// "Anunțurile mele", or close and keep editing (also Escape or a click outside). A native <dialog>:
// the browser traps focus, and gives it back to the form when closed.
export function ListingSavedDialog({
  listingId,
  savedAt,
  status,
  wasRejected,
  priceReduction,
}: {
  listingId: string;
  savedAt?: number;
  status?: string;
  wasRejected: boolean;
  priceReduction?: PriceReduction | null;
}) {
  const t = useTranslations("EditListingPage.savedDialog");
  const tPrice = useTranslations("PriceHistory");
  const dialog = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    if (savedAt && !dialog.current?.open) dialog.current?.showModal();
  }, [savedAt]);

  if (!status) return null;
  const outcome = savedOutcome(status, wasRejected);
  const close = () => dialog.current?.close();

  return (
    <dialog
      ref={dialog}
      aria-labelledby="listing-saved-title"
      aria-describedby="listing-saved-body"
      onClick={(e) => e.target === dialog.current && close()}
      className="m-auto w-[calc(100%-2rem)] max-w-md rounded-2xl bg-white p-0 text-left shadow-xl backdrop:bg-ink-950/50"
    >
      <div className="flex flex-col items-center gap-3 p-6 text-center sm:p-7">
        <OutcomeIcon outcome={outcome} />
        <h2 id="listing-saved-title" className="font-display text-xl font-medium text-ink-950">
          {t(`${outcome}Title`)}
        </h2>
        <p id="listing-saved-body" className="text-sm text-ink-600">
          {t(`${outcome}Body`)}
        </p>
        {outcome === "live" && priceReduction && (
          <p className="rounded-xl bg-emerald-50 px-3.5 py-2.5 text-sm text-emerald-800">
            {t("priceReduced", {
              badge: tPrice("badge", { percent: formatPercentChange(-priceReduction.percent) ?? "" }),
              price: formatPrice(priceReduction.previousAmount, priceReduction.previousCurrency),
            })}
          </p>
        )}

        {/* The two ways on — the listing (focused) and "Anunțurile mele": stacked on a phone with
            the main one on top, side by side and equally wide on a wider screen (main one on the
            right). Staying on the form is the quiet option under them. */}
        <div className="mt-3 flex w-full flex-col gap-2 sm:flex-row-reverse">
          <LinkButton href={`/property/${listingId}`} autoFocus className="w-full whitespace-nowrap sm:flex-1">
            {t("viewListing")}
          </LinkButton>
          <LinkButton href="/my-listings" variant="secondary" className="w-full whitespace-nowrap sm:flex-1">
            {t("myListings")}
          </LinkButton>
        </div>
        <Button type="button" variant="ghost" size="sm" onClick={close}>
          {t("keepEditing")}
        </Button>
      </div>
    </dialog>
  );
}

function OutcomeIcon({ outcome }: { outcome: SavedOutcome }) {
  const [tone, path] =
    outcome === "live"
      ? ["bg-emerald-50 text-emerald-700", "m5 12.5 4.5 4.5L19 7.5"]
      : outcome === "resubmitted" || outcome === "inReview"
        ? ["bg-brand-50 text-brand-700", "M12 7v5l3 2M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"]
        : outcome === "notPublic"
          ? ["bg-ink-100 text-ink-600", "m5 12.5 4.5 4.5L19 7.5"]
          : ["bg-accent-100 text-accent-700", "M4 6h16v12H4zM4 7l8 6 8-6"];
  return (
    <span className={`flex h-12 w-12 items-center justify-center rounded-full ${tone}`}>
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-6 w-6" aria-hidden>
        <path d={path} strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    </span>
  );
}
