import { getTranslations } from "next-intl/server";

const ITEMS = [
  { key: "verified", d: "M12 2 4 6v6c0 5 3.4 8.6 8 10 4.6-1.4 8-5 8-10V6l-8-4Z" },
  { key: "updatedDaily", d: "M12 8v4l3 3M12 3a9 9 0 1 0 9 9" },
  { key: "transparentPricing", d: "M20.6 10.6 12 2H4v8l8.6 8.6a2 2 0 0 0 2.8 0l5.2-5.2a2 2 0 0 0 0-2.8ZM7 7h.01" },
] as const;

export async function TrustRow() {
  const t = await getTranslations("Hero.trust");

  return (
    <div className="flex flex-wrap items-center justify-center gap-x-10 gap-y-4">
      {ITEMS.map((item) => (
        <div key={item.key} className="flex items-center gap-2.5">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-brand-50 text-brand-600">
            <svg
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.8"
              strokeLinecap="round"
              strokeLinejoin="round"
              className="h-4.5 w-4.5"
            >
              <path d={item.d} />
            </svg>
          </span>
          <span className="text-sm font-semibold text-ink-800">{t(item.key)}</span>
        </div>
      ))}
    </div>
  );
}
