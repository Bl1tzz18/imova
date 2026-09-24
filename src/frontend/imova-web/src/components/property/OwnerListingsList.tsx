"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Badge } from "@/components/ui/Badge";
import { Button, LinkButton } from "@/components/ui/Button";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { archiveListing, publishListing, submitForReview } from "@/lib/property/actions";
import { formatLocation, formatPrice } from "@/lib/utils/format";
import { cn } from "@/lib/utils/cn";
import { coverPhoto } from "@/lib/listing/view";
import type { Listing } from "@/types/listing";

type StatusFilter = "all" | "PendingReview" | "Rejected" | "Active" | "Draft" | "Archived";

// "accent" is this app's alert/attention color (same tone used for validation and error
// banners elsewhere), so it's reserved for statuses that need the owner to act — Rejected and
// Suspended — rather than for "live" statuses, so a rejected listing doesn't blend into the
// rest of the gray/neutral bucket.
const statusBadgeTone: Record<string, "brand" | "accent" | "neutral"> = {
  Active: "brand",
  Rented: "brand",
  Sold: "brand",
  Draft: "neutral",
  PendingReview: "neutral",
  Rejected: "accent",
  Suspended: "accent",
  Archived: "neutral",
  Expired: "neutral",
};

// Archive() (the "deactivate" action) accepts these statuses — see Listing.Archive()'s guard.
const ARCHIVABLE_STATUSES = new Set(["Active", "Rented", "Sold", "Expired"]);

// Publish() (the owner's "activate" action) accepts these — see Listing.Publish()'s guard.
const REPUBLISHABLE_STATUSES = new Set(["Archived", "Expired"]);

export function OwnerListingsList({ listings }: { listings: Listing[] }) {
  const t = useTranslations("MyListingsPage");
  const [filter, setFilter] = useState<StatusFilter>("all");
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [, startTransition] = useTransition();

  const tabs: { id: StatusFilter; label: string }[] = [
    { id: "all", label: t("tabAll") },
    { id: "Active", label: t("tabPublished") },
    { id: "Draft", label: t("tabDraft") },
    { id: "PendingReview", label: t("tabPendingReview") },
    { id: "Rejected", label: t("tabRejected") },
    { id: "Archived", label: t("tabArchived") },
  ];

  const filtered = filter === "all" ? listings : listings.filter((p) => p.status === filter);

  function runAction(listingId: string, action: (id: string) => Promise<{ error?: string }>) {
    setError(null);
    setPendingId(listingId);
    startTransition(async () => {
      const result = await action(listingId);
      setPendingId(null);
      if (result.error) {
        setError(result.error);
      }
    });
  }

  function handleConfirmDeactivate(listingId: string) {
    setConfirmingId(null);
    runAction(listingId, archiveListing);
  }

  return (
    <div>
      <div className="flex gap-1 overflow-x-auto border-b border-ink-100">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            onClick={() => setFilter(tab.id)}
            className={cn(
              "whitespace-nowrap border-b-2 px-3 py-2.5 text-sm font-medium transition-colors",
              filter === tab.id
                ? "border-brand-600 text-ink-950"
                : "border-transparent text-ink-500 hover:text-ink-800",
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {error && (
        <p className="mt-4 rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
          {error}
        </p>
      )}

      {filtered.length > 0 ? (
        <div className="mt-5 flex flex-col gap-3">
          {filtered.map((listing) => {
            const location = formatLocation(listing.property.location);
            const cover = coverPhoto(listing);
            const pending = pendingId === listing.id;
            const reason = listing.status === "Rejected"
              ? listing.rejectionReason
              : listing.status === "Suspended"
                ? listing.suspensionReason
                : null;

            return (
              <div
                key={listing.id}
                className="flex flex-col gap-3 rounded-2xl border border-ink-100 bg-white p-3.5"
              >
                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:gap-4">
                  <Link
                    href={`/property/${listing.id}`}
                    className="flex h-[68px] w-[88px] shrink-0 items-center justify-center overflow-hidden rounded-xl bg-gradient-to-br from-brand-800 to-brand-600"
                  >
                    {cover ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img
                        src={cover.url}
                        alt={listing.title}
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <PropertyIcon type={listing.property.propertyType} className="h-7 w-7 text-white/40" />
                    )}
                  </Link>

                  <div className="min-w-0 flex-1">
                    <Link
                      href={`/property/${listing.id}`}
                      className="block truncate text-sm font-medium text-ink-900 hover:underline"
                    >
                      {listing.title}
                    </Link>
                    {location && <p className="mt-0.5 truncate text-xs text-ink-500">{location}</p>}
                  </div>

                  <p className="font-display text-base font-semibold text-ink-950 sm:whitespace-nowrap">
                    {formatPrice(listing.price.amount, listing.price.currency)}
                  </p>

                  <Badge tone={statusBadgeTone[listing.status] ?? "neutral"} className="shrink-0">
                    {statusLabel(t, listing.status)}
                  </Badge>

                  {ARCHIVABLE_STATUSES.has(listing.status) && confirmingId === listing.id ? (
                    <div className="flex shrink-0 flex-wrap items-center gap-2">
                      <span className="text-xs text-ink-600">{t("deactivateConfirm")}</span>
                      <Button
                        variant="primary"
                        size="sm"
                        disabled={pending}
                        onClick={() => handleConfirmDeactivate(listing.id)}
                      >
                        {t("deactivateConfirmYes")}
                      </Button>
                      <Button variant="ghost" size="sm" onClick={() => setConfirmingId(null)}>
                        {t("deactivateConfirmCancel")}
                      </Button>
                    </div>
                  ) : (
                    <div className="flex shrink-0 gap-2">
                      <LinkButton
                        href={`/my-listings/${listing.id}/edit`}
                        variant={listing.status === "Rejected" ? "primary" : "secondary"}
                        size="sm"
                      >
                        {listing.status === "Rejected" ? t("editAndResubmit") : t("edit")}
                      </LinkButton>

                      {listing.status === "Draft" && (
                        <Button
                          variant="primary"
                          size="sm"
                          disabled={pending}
                          onClick={() => runAction(listing.id, submitForReview)}
                        >
                          {t("submitForReview")}
                        </Button>
                      )}
                      {ARCHIVABLE_STATUSES.has(listing.status) && (
                        <Button
                          variant="secondary"
                          size="sm"
                          disabled={pending}
                          onClick={() => setConfirmingId(listing.id)}
                        >
                          {t("deactivate")}
                        </Button>
                      )}
                      {REPUBLISHABLE_STATUSES.has(listing.status) && (
                        <Button
                          variant="secondary"
                          size="sm"
                          disabled={pending}
                          onClick={() => runAction(listing.id, publishListing)}
                        >
                          {t("activate")}
                        </Button>
                      )}
                    </div>
                  )}
                </div>

                {reason && (
                  <div className="rounded-xl border border-accent-100 bg-accent-100/60 px-3.5 py-2.5 text-sm text-accent-700">
                    <p className="font-medium">
                      {listing.status === "Rejected" ? t("rejectionReasonLabel") : t("suspensionReasonLabel")}
                    </p>
                    <p className="mt-0.5">{reason}</p>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      ) : (
        <div className="mt-8 flex flex-col items-center gap-2 rounded-2xl border border-dashed border-ink-200 bg-white px-6 py-16 text-center">
          <p className="text-sm font-medium text-ink-700">
            {filter === "all" ? t("emptyTitle") : t("emptyTitleFiltered")}
          </p>
          <p className="text-sm text-ink-500">
            {filter === "all" ? t("emptyBody") : t("emptyBodyFiltered")}
          </p>
        </div>
      )}
    </div>
  );
}

function statusLabel(t: ReturnType<typeof useTranslations<"MyListingsPage">>, status: string) {
  switch (status) {
    case "Draft":
      return t("statusDraft");
    case "PendingReview":
      return t("statusPendingReview");
    case "Active":
      return t("statusPublished");
    case "Rejected":
      return t("statusRejected");
    case "Suspended":
      return t("statusSuspended");
    case "Rented":
      return t("statusRented");
    case "Sold":
      return t("statusSold");
    case "Archived":
      return t("statusArchived");
    case "Expired":
      return t("statusExpired");
    default:
      return status;
  }
}
