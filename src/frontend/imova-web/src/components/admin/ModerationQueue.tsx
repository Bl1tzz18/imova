"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { TextAreaInput } from "@/components/ui/Field";
import { PropertyIcon } from "@/components/property/PropertyIcon";
import { approveListing, rejectListing } from "@/lib/admin/actions";
import { formatLocation, formatPrice } from "@/lib/utils/format";
import type { Property } from "@/types/property";

export function ModerationQueue({ properties }: { properties: Property[] }) {
  const t = useTranslations("AdminModerationPage");
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [reason, setReason] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [, startTransition] = useTransition();

  function handleApprove(propertyId: string) {
    setErrors((prev) => ({ ...prev, [propertyId]: "" }));
    setPendingId(propertyId);
    startTransition(async () => {
      const result = await approveListing(propertyId);
      setPendingId(null);
      if (result.error) {
        setErrors((prev) => ({ ...prev, [propertyId]: result.error! }));
      }
    });
  }

  function handleStartReject(propertyId: string) {
    setRejectingId(propertyId);
    setReason("");
  }

  function handleConfirmReject(propertyId: string) {
    const trimmed = reason.trim();
    if (!trimmed) {
      setErrors((prev) => ({ ...prev, [propertyId]: t("reasonRequired") }));
      return;
    }

    setErrors((prev) => ({ ...prev, [propertyId]: "" }));
    setPendingId(propertyId);
    setRejectingId(null);
    startTransition(async () => {
      const result = await rejectListing(propertyId, trimmed);
      setPendingId(null);
      if (result.error) {
        setErrors((prev) => ({ ...prev, [propertyId]: result.error! }));
      }
    });
  }

  return (
    <div className="flex flex-col gap-3">
      {properties.map((property) => {
        const location = formatLocation(property.location);
        const pending = pendingId === property.id;
        const rejecting = rejectingId === property.id;
        const error = errors[property.id];

        return (
          <div
            key={property.id}
            className="flex flex-col gap-3 rounded-2xl border border-ink-100 bg-white p-3.5"
          >
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:gap-4">
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
              </div>

              <p className="font-display text-base font-semibold text-ink-950 sm:whitespace-nowrap">
                {formatPrice(property.price, property.currency)}
              </p>

              {!rejecting && (
                <div className="flex shrink-0 gap-2">
                  <Button variant="primary" size="sm" disabled={pending} onClick={() => handleApprove(property.id)}>
                    {t("approve")}
                  </Button>
                  <Button
                    variant="secondary"
                    size="sm"
                    disabled={pending}
                    onClick={() => handleStartReject(property.id)}
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
                  <Button variant="primary" size="sm" onClick={() => handleConfirmReject(property.id)}>
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
