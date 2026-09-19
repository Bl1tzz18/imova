"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Badge } from "@/components/ui/Badge";
import { Button, LinkButton } from "@/components/ui/Button";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { archiveProperty, republishProperty, submitForReview } from "@/lib/property/actions";
import { formatLocation, formatPrice } from "@/lib/utils/format";
import { cn } from "@/lib/utils/cn";
import type { Property } from "@/types/property";

type StatusFilter = "all" | "PendingReview" | "Published" | "Draft" | "Archived";

const statusBadgeTone: Record<string, "brand" | "accent" | "neutral"> = {
  Published: "accent",
  Rented: "accent",
  Sold: "accent",
  Draft: "brand",
  PendingReview: "brand",
  Rejected: "neutral",
  Suspended: "neutral",
  Archived: "neutral",
};

// Archive() (the "deactivate" action) accepts these three statuses — see Property.Archive()'s guard.
const ARCHIVABLE_STATUSES = new Set(["Published", "Rented", "Sold"]);

export function OwnerListingsList({ properties }: { properties: Property[] }) {
  const t = useTranslations("MyListingsPage");
  const [filter, setFilter] = useState<StatusFilter>("all");
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [, startTransition] = useTransition();

  const tabs: { id: StatusFilter; label: string }[] = [
    { id: "all", label: t("tabAll") },
    { id: "PendingReview", label: t("tabPendingReview") },
    { id: "Published", label: t("tabPublished") },
    { id: "Draft", label: t("tabDraft") },
    { id: "Archived", label: t("tabArchived") },
  ];

  const filtered = filter === "all" ? properties : properties.filter((p) => p.status === filter);

  function runAction(propertyId: string, action: (id: string) => Promise<{ error?: string }>) {
    setError(null);
    setPendingId(propertyId);
    startTransition(async () => {
      const result = await action(propertyId);
      setPendingId(null);
      if (result.error) {
        setError(result.error);
      }
    });
  }

  function handleConfirmDeactivate(propertyId: string) {
    setConfirmingId(null);
    runAction(propertyId, archiveProperty);
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
          {filtered.map((property) => {
            const location = formatLocation(property.location);
            const pending = pendingId === property.id;
            const reason = property.status === "Rejected"
              ? property.rejectionReason
              : property.status === "Suspended"
                ? property.suspensionReason
                : null;

            return (
              <div
                key={property.id}
                className="flex flex-col gap-3 rounded-2xl border border-ink-100 bg-white p-3.5 sm:flex-row sm:items-center sm:gap-4"
              >
                <Link
                  href={`/property/${property.id}`}
                  className="flex h-[68px] w-[88px] shrink-0 items-center justify-center overflow-hidden rounded-xl bg-gradient-to-br from-brand-800 to-brand-600"
                >
                  {property.media.length > 0 ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img
                      src={property.media[0].url}
                      alt={property.title}
                      className="h-full w-full object-cover"
                    />
                  ) : (
                    <PropertyIcon type={property.propertyType} className="h-7 w-7 text-white/40" />
                  )}
                </Link>

                <div className="min-w-0 flex-1">
                  <Link
                    href={`/property/${property.id}`}
                    className="block truncate text-sm font-medium text-ink-900 hover:underline"
                  >
                    {property.title}
                  </Link>
                  {location && <p className="mt-0.5 truncate text-xs text-ink-500">{location}</p>}
                  {reason && <p className="mt-0.5 truncate text-xs text-accent-600">{reason}</p>}
                </div>

                <p className="font-display text-base font-semibold text-ink-950 sm:whitespace-nowrap">
                  {formatPrice(property.price, property.currency)}
                </p>

                <Badge tone={statusBadgeTone[property.status] ?? "neutral"} className="shrink-0">
                  {statusLabel(t, property.status)}
                </Badge>

                {ARCHIVABLE_STATUSES.has(property.status) && confirmingId === property.id ? (
                  <div className="flex shrink-0 flex-wrap items-center gap-2">
                    <span className="text-xs text-ink-600">{t("deactivateConfirm")}</span>
                    <Button
                      variant="primary"
                      size="sm"
                      disabled={pending}
                      onClick={() => handleConfirmDeactivate(property.id)}
                    >
                      {t("deactivateConfirmYes")}
                    </Button>
                    <Button variant="ghost" size="sm" onClick={() => setConfirmingId(null)}>
                      {t("deactivateConfirmCancel")}
                    </Button>
                  </div>
                ) : (
                  <div className="flex shrink-0 gap-2">
                    <LinkButton href={`/my-listings/${property.id}/edit`} variant="secondary" size="sm">
                      {t("edit")}
                    </LinkButton>

                    {(property.status === "Draft" || property.status === "Rejected") && (
                      <Button
                        variant="primary"
                        size="sm"
                        disabled={pending}
                        onClick={() => runAction(property.id, submitForReview)}
                      >
                        {t("submitForReview")}
                      </Button>
                    )}
                    {ARCHIVABLE_STATUSES.has(property.status) && (
                      <Button
                        variant="secondary"
                        size="sm"
                        disabled={pending}
                        onClick={() => setConfirmingId(property.id)}
                      >
                        {t("deactivate")}
                      </Button>
                    )}
                    {property.status === "Archived" && (
                      <Button
                        variant="secondary"
                        size="sm"
                        disabled={pending}
                        onClick={() => runAction(property.id, republishProperty)}
                      >
                        {t("activate")}
                      </Button>
                    )}
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
    case "Published":
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
    default:
      return status;
  }
}
