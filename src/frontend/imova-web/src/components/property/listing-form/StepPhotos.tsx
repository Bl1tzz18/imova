"use client";

import { useTranslations } from "next-intl";
import { ImageUploader } from "@/components/property/ImageUploader";
import type { PropertyMedia } from "@/types/property";

export function StepPhotos({
  propertyId,
  initialMedia,
}: {
  propertyId: string | null;
  initialMedia?: PropertyMedia[];
}) {
  const t = useTranslations("PropertyForm");

  return (
    <div>
      <h2 className="font-display text-xl font-medium text-ink-950">{t("step3Heading")}</h2>
      <div className="mt-5">
        {propertyId && <ImageUploader propertyId={propertyId} initialMedia={initialMedia} />}
      </div>
    </div>
  );
}
