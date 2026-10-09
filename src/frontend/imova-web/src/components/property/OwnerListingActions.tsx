"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { Button, LinkButton } from "@/components/ui/Button";
import { DropdownMenu } from "@/components/ui/DropdownMenu";
import {
  archiveListing,
  markListingAsRented,
  markListingAsSold,
  publishListing,
  renewListing,
  submitForReview,
} from "@/lib/property/actions";
import { CONFIRMED_ACTIONS, type OwnerAction } from "@/lib/listing/ownerActions";
import { arrangeOwnerActions } from "@/lib/listing/ownerGroups";
import { cn } from "@/lib/utils/cn";

const RUN: Partial<Record<OwnerAction, (id: string) => Promise<{ error?: string }>>> = {
  submitForReview,
  renew: renewListing,
  markAsSold: markListingAsSold,
  markAsRented: markListingAsRented,
  deactivate: archiveListing,
  activate: publishListing,
};

// The owner's buttons for one listing — the same in "Anunțurile mele" and in the bar on the
// listing's page: the next step filled (renew, submit, resubmit, activate), Edit beside it, and the
// rarely used rest (sold/rented, deactivate) in a "⋯" menu. Taking it off the site asks first.
// After an action the page refreshes, so the status and buttons follow. `stretch`: the buttons
// share a full-width row (phones).
export function OwnerListingActions({
  listingId,
  actions,
  stretch = false,
  className,
}: {
  listingId: string;
  actions: OwnerAction[];
  stretch?: boolean;
  className?: string;
}) {
  const t = useTranslations("MyListingsPage");
  const router = useRouter();
  const [confirming, setConfirming] = useState<OwnerAction | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();
  const { primary, edit, more } = arrangeOwnerActions(actions);

  function choose(action: OwnerAction) {
    if (CONFIRMED_ACTIONS.has(action)) {
      setConfirming(action);
      return;
    }
    run(action);
  }

  function run(action: OwnerAction) {
    const fn = RUN[action];
    if (!fn) return;
    setConfirming(null);
    setError(null);
    startTransition(async () => {
      const result = await fn(listingId);
      if (result.error) {
        setError(result.error);
        return;
      }
      router.refresh();
    });
  }

  function button(action: OwnerAction, variant: "primary" | "secondary") {
    const cls = cn(stretch && "flex-1");
    return action === "edit" || action === "editAndResubmit" || action === "addPhotos" ? (
      <LinkButton
        key={action}
        // "Adaugă fotografii" opens the form right on its photos step.
        href={`/my-listings/${listingId}/edit${action === "addPhotos" ? "?step=photos" : ""}`}
        variant={variant}
        size="sm"
        className={cls}
      >
        {t(action)}
      </LinkButton>
    ) : (
      <Button key={action} variant={variant} size="sm" disabled={pending} onClick={() => choose(action)} className={cls}>
        {t(action)}
      </Button>
    );
  }

  if (confirming) {
    return (
      <div className={cn("rounded-xl bg-ink-50 px-3 py-2.5", className)} role="alertdialog" aria-label={t(`confirm.${confirming}`)}>
        <p className="text-sm text-ink-800">{t(`confirm.${confirming}`)}</p>
        <div className="mt-2 flex gap-2">
          <Button variant="primary" size="sm" disabled={pending} onClick={() => run(confirming)} className={cn(stretch && "flex-1")}>
            {t(`confirmYes.${confirming}`)}
          </Button>
          <Button variant="secondary" size="sm" onClick={() => setConfirming(null)} className={cn(stretch && "flex-1")}>
            {t("deactivateConfirmCancel")}
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className={className}>
      <div className="flex items-center gap-2">
        {primary && button(primary, "primary")}
        {edit && button(edit, "secondary")}
        {more.length > 0 && (
          <DropdownMenu
            label={t("moreActions")}
            disabled={pending}
            items={more.map((action) => ({ id: action, label: t(action), onSelect: () => choose(action) }))}
            triggerClassName="flex h-9 w-9 items-center justify-center rounded-full border border-ink-200 bg-white text-ink-700 transition-colors hover:border-ink-300 hover:bg-ink-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600 disabled:opacity-50"
            trigger={
              <svg viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4" aria-hidden>
                <circle cx="5" cy="12" r="1.8" />
                <circle cx="12" cy="12" r="1.8" />
                <circle cx="19" cy="12" r="1.8" />
              </svg>
            }
          />
        )}
      </div>
      {error && (
        <p role="alert" className="mt-2 text-sm text-accent-700">
          {error}
        </p>
      )}
    </div>
  );
}
