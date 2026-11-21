"use client";

import { useTranslations } from "next-intl";
import { ImageUploader } from "@/components/property/ImageUploader";
import { MIN_LISTING_PHOTOS, RECOMMENDED_LISTING_PHOTOS, type PhotoRequirement } from "@/lib/property/photos";
import { cn } from "@/lib/utils/cn";
import type { Photo } from "@/types/listing";

// The photos step. Any number of photos, at least MIN_LISTING_PHOTOS — the wording says both, so the
// minimum doesn't read as the number to add. A live counter says how many are there and how many are
// missing; the form won't go on (or save) before — `showError` is set when it
// tried, and the message is announced next to the photos, not in a toast.
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
  const filled = Math.min(requirement.count, MIN_LISTING_PHOTOS);

  return (
    <div>
      <h2 className="font-hero text-xl font-bold text-ink-950">{t("step3Heading")}</h2>
      <p className="mt-1 text-sm text-ink-500">{t("photosRule", { min: MIN_LISTING_PHOTOS })}</p>

      <div className="mt-4 flex items-center gap-3" aria-live="polite">
        {requirement.missing > 0 ? (
          // Progress towards the minimum only — once it's reached the bar gives way to a tick, so
          // three full segments never read as "all the slots are used".
          <div className="flex gap-1" aria-hidden>
            {Array.from({ length: MIN_LISTING_PHOTOS }, (_, i) => (
              <span key={i} className={cn("h-1.5 w-8 rounded-full", i < filled ? "bg-brand-600" : "bg-ink-200")} />
            ))}
          </div>
        ) : (
          <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-brand-600 text-white" aria-hidden>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" className="h-3.5 w-3.5">
              <path d="m5 12 5 5 9-10" />
            </svg>
          </span>
        )}
        <p className={cn("text-sm font-medium", requirement.missing === 0 ? "text-brand-700" : "text-ink-700")}>
          {requirement.missing === 0
            ? t("photosEnough", { count: requirement.count })
            : t("photosMissing", { count: requirement.count, min: MIN_LISTING_PHOTOS, missing: requirement.missing })}
        </p>
      </div>
      {requirement.missing === 0 && (
        <p className="mt-1 text-xs text-ink-500">
          {requirement.count < RECOMMENDED_LISTING_PHOTOS
            ? t("photosRecommend", { recommended: RECOMMENDED_LISTING_PHOTOS })
            : t("photosMore")}
        </p>
      )}

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
