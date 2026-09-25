"use client";

import { useTranslations } from "next-intl";

type TipIconName = "camera" | "tag" | "text";

export function ListingTips() {
  const t = useTranslations("PropertyForm");

  const tips: { icon: TipIconName; text: string }[] = [
    { icon: "camera", text: t("tipPhotos") },
    { icon: "tag", text: t("tipPrice") },
    { icon: "text", text: t("tipDescription") },
  ];

  return (
    <div className="rounded-2xl border border-accent-100 bg-accent-100/30 p-6">
      <h3 className="font-hero text-base font-bold text-ink-950">{t("tipsHeading")}</h3>
      <div className="mt-4 space-y-3.5">
        {tips.map((tip) => (
          <div key={tip.icon} className="flex items-start gap-2.5">
            <TipIcon icon={tip.icon} />
            <span className="text-[13px] leading-relaxed text-ink-900">{tip.text}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

function TipIcon({ icon }: { icon: TipIconName }) {
  const className = "mt-0.5 h-[17px] w-[17px] shrink-0 text-accent-600";

  if (icon === "camera") {
    return (
      <svg viewBox="0 0 24 24" className={className} fill="none" stroke="currentColor" strokeWidth="1.6">
        <rect x="3" y="7" width="18" height="13" rx="2" />
        <path d="M8 7l1.6-2.5h4.8L16 7" strokeLinejoin="round" />
        <circle cx="12" cy="13.5" r="3.4" />
      </svg>
    );
  }

  if (icon === "tag") {
    return (
      <svg viewBox="0 0 24 24" className={className} fill="none" stroke="currentColor" strokeWidth="1.6">
        <path d="M11.5 3.5H5v6.5L14.5 20l6.5-6.5L11.5 3.5Z" strokeLinejoin="round" />
        <circle cx="8.7" cy="7.2" r="1.2" fill="currentColor" stroke="none" />
      </svg>
    );
  }

  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" stroke="currentColor" strokeWidth="1.6">
      <line x1="5" y1="7" x2="19" y2="7" strokeLinecap="round" />
      <line x1="5" y1="12" x2="19" y2="12" strokeLinecap="round" />
      <line x1="5" y1="17" x2="13" y2="17" strokeLinecap="round" />
    </svg>
  );
}
