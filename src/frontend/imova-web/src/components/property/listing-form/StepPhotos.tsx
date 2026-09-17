"use client";

import { useTranslations } from "next-intl";
import { ImageUploader } from "@/components/property/ImageUploader";

export function StepPhotos({ propertyId }: { propertyId: string | null }) {
  const t = useTranslations("PropertyForm");

  return (
    <div>
      <h2 className="font-display text-xl font-medium text-ink-950">{t("step3Heading")}</h2>
      <div className="mt-5">{propertyId && <ImageUploader propertyId={propertyId} />}</div>
    </div>
  );
}
