"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { TextAreaInput } from "@/components/ui/Field";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { approveListing, rejectListing } from "@/lib/admin/actions";
import { formatLocation, formatPrice } from "@/lib/utils/format";
import { coverPhoto } from "@/lib/listing/view";
import type { Listing } from "@/types/listing";

export function ModerationQueue({ listings }: { listings: Listing[] }) {
  const t = useTranslations("AdminModerationPage");
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [reason, setReason] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [, startTransition] = useTransition();

  function handleApprove(listingId: string) {
    setErrors((prev) => ({ ...prev, [listingId]: "" }));
    setPendingId(listingId);
    startTransition(async () => {
      const result = await approveListing(listingId);
      setPendingId(null);
      if (result.error) {
        setErrors((prev) => ({ ...prev, [listingId]: result.error! }));
      }
    });
  }

  function handleStartReject(listingId: string) {
    setRejectingId(listingId);
    setReason("");
  }

  function handleConfirmReject(listingId: string) {
    const trimmed = reason.trim();
    if (!trimmed) {
      setErrors((prev) => ({ ...prev, [listingId]: t("reasonRequired") }));
      return;
    }

    setErrors((prev) => ({ ...prev, [listingId]: "" }));
    setPendingId(listingId);
    setRejectingId(null);
    startTransition(async () => {
      const result = await rejectListing(listingId, trimmed);
      setPendingId(null);
      if (result.error) {
        setErrors((prev) => ({ ...prev, [listingId]: result.error! }));
      }
    });
  }

  return (
    <div className="flex flex-col gap-3">
      {listings.map((listing) => {
        const location = formatLocation(listing.property.location);
        const cover = coverPhoto(listing);
        const pending = pendingId === listing.id;
        const rejecting = rejectingId === listing.id;
        const error = errors[listing.id];

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

              {!rejecting && (
                <div className="flex shrink-0 gap-2">
                  <Button variant="primary" size="sm" disabled={pending} onClick={() => handleApprove(listing.id)}>
                    {t("approve")}
                  </Button>
                  <Button
                    variant="secondary"
                    size="sm"
                    disabled={pending}
                    onClick={() => handleStartReject(listing.id)}
                  >
                    {t("reject")}
                  </Button>
                </div>
              )}
            </div>

            {rejecting && (
              <div className="flex flex-col gap-2 rounded-xl border border-ink-100 bg-ink-50/50 p-3">
                <TextAreaInput
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  placeholder={t("reasonPlaceholder")}
                  rows={3}
                  maxLength={1000}
                />
                <div className="flex gap-2">
                  <Button variant="primary" size="sm" onClick={() => handleConfirmReject(listing.id)}>
                    {t("confirmReject")}
                  </Button>
                  <Button variant="ghost" size="sm" onClick={() => setRejectingId(null)}>
                    {t("cancel")}
                  </Button>
                </div>
              </div>
            )}

            {error && <p className="text-xs text-accent-600">{error}</p>}
          </div>
        );
      })}
    </div>
  );
}
