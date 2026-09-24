"use client";

import { useTranslations } from "next-intl";
import { ImageUploader } from "@/components/property/ImageUploader";
import type { Photo } from "@/types/listing";

export function StepPhotos({
  listingId,
  initialPhotos,
  deferDeletes,
}: {
  listingId: string | null;
  initialPhotos?: Photo[];
  deferDeletes?: boolean;
}) {
  const t = useTranslations("PropertyForm");

  return (
    <div>
      <h2 className="font-display text-xl font-medium text-ink-950">{t("step3Heading")}</h2>
      <div className="mt-5">
        {listingId && (
          <ImageUploader listingId={listingId} initialPhotos={initialPhotos} deferDeletes={deferDeletes} />
        )}
      </div>
    </div>
  );
}
