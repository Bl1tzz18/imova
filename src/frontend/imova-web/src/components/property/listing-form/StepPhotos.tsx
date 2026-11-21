"use client";

import { useTranslations } from "next-intl";
import { ImageUploader } from "@/components/property/ImageUploader";
import { MIN_LISTING_PHOTOS, RECOMMENDED_LISTING_PHOTOS, photoMeter, type PhotoRequirement } from "@/lib/property/photos";
import { cn } from "@/lib/utils/cn";
import type { Photo } from "@/types/listing";

// The photos step. Any number of photos, at least MIN_LISTING_PHOTOS. People skim here, so the
// minimum is shown as a mark on an open-ended meter (photoMeter), next to a running count and one
// status line — never as a fixed number of slots. The form won't go on (or save) below the minimum;
// `showError` is set when it tried, and the message is announced next to the photos, not in a toast.
export function StepPhotos({
  listingId,
  initialPhotos,
  deferDeletes,
  requirement,
  showError,
  onCountChange,
}: {
  listingId: string | null;
  initialPhotos?: Photo[];
  deferDeletes?: boolean;
  requirement: PhotoRequirement;
  showError: boolean;
  onCountChange: (count: number, uploading: boolean) => void;
}) {
  const t = useTranslations("PropertyForm");
  const meter = photoMeter(requirement.count);

  return (
    <div>
      <div className="flex items-baseline justify-between gap-3">
        <h2 className="font-hero text-xl font-bold text-ink-950">{t("step3Heading")}</h2>
        <span className="shrink-0 text-sm font-medium tabular-nums text-ink-500">{t("photosAdded", { count: requirement.count })}</span>
      </div>
      <p className="mt-1 text-sm text-ink-500">{t("photosIntro")}</p>

      {/* Open-ended meter: the minimum and the recommendation are marks along a bar that fades out
          past its end, so "3" reads as a threshold to pass, never as the number of slots. */}
      <div className="mt-4" aria-hidden>
        <div className="relative h-2 rounded-full bg-gradient-to-r from-ink-200 from-80% to-transparent">
          <div
            className={cn(
              "absolute inset-y-0 left-0 rounded-full transition-[width] duration-500 ease-out",
              meter.stage === "belowMinimum" ? "bg-accent-500" : "bg-brand-600",
            )}
            style={{ width: `${meter.fillPercent}%` }}
          />
          {[meter.minPercent, meter.recommendedPercent].map((at) => (
            <span key={at} className="absolute -top-1 h-4 w-0.5 -translate-x-1/2 rounded-full bg-ink-400" style={{ left: `${at}%` }} />
          ))}
        </div>
        <div className="relative mt-1.5 h-4 text-[11px] font-medium text-ink-500">
          <span className="absolute -translate-x-1/2 whitespace-nowrap" style={{ left: `${meter.minPercent}%` }}>
            {t("photosMarkMin", { min: MIN_LISTING_PHOTOS })}
          </span>
          <span className="absolute -translate-x-1/2 whitespace-nowrap" style={{ left: `${meter.recommendedPercent}%` }}>
            {t("photosMarkIdeal", { recommended: RECOMMENDED_LISTING_PHOTOS })}
          </span>
        </div>
      </div>

      <p
        aria-live="polite"
        className={cn(
          "mt-2 flex items-start gap-2 text-sm font-medium",
          meter.stage === "belowMinimum" ? "text-ink-700" : "text-brand-700",
        )}
      >
        {meter.stage !== "belowMinimum" && (
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" className="mt-0.5 h-4 w-4 shrink-0" aria-hidden>
            <path d="m5 12 5 5 9-10" />
          </svg>
        )}
        {meter.stage === "belowMinimum"
          ? t("photosStatusBelow", { missing: requirement.missing })
          : meter.stage === "enough"
            ? t("photosStatusEnough")
            : t("photosStatusPlenty")}
      </p>

      {showError && !requirement.met && (
        <p role="alert" className="mt-3 rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
          {requirement.missing > 0
            ? t("photosNeeded", { min: MIN_LISTING_PHOTOS, missing: requirement.missing })
            : t("photosStillUploading")}
        </p>
      )}

      <div className="mt-5">
        {listingId && (
          <ImageUploader listingId={listingId} initialPhotos={initialPhotos} deferDeletes={deferDeletes} onCountChange={onCountChange} />
        )}
      </div>
    </div>
  );
}
