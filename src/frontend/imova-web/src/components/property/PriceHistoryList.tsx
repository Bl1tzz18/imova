"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatPercentChange, priceHistoryRows, PRICE_HISTORY_PREVIEW_ROWS } from "@/lib/listing/priceHistory";
import { formatDate, formatPrice } from "@/lib/utils/format";
import type { PriceHistory } from "@/types/listing";

// The listing's price over time, in the price card: newest first, each line the date, the price
// and the change (green when cheaper), ending with the price it was published at. Long histories
// show the latest few lines until "Show all".
export function PriceHistoryList({ history, publishedAt }: { history: PriceHistory; publishedAt: string | null }) {
  const t = useTranslations("PriceHistory");
  const locale = useLocale();
  const [expanded, setExpanded] = useState(false);
  const rows = priceHistoryRows(history, publishedAt);
  if (rows.length === 0) return null;
  const shown = expanded ? rows : rows.slice(0, PRICE_HISTORY_PREVIEW_ROWS);

  return (
    <section aria-labelledby="price-history-title" className="mt-4 border-t border-ink-100 pt-4">
      <h3 id="price-history-title" className="text-sm font-medium text-ink-900">
        {t("title")}
      </h3>
      <ol className="mt-2 space-y-1.5 text-sm">
        {shown.map((row, i) => {
          const percent = row.kind === "change" ? formatPercentChange(row.changePercent) : null;
          const label =
            row.kind === "change"
              ? formatDate(row.date, locale)
              : row.kind === "published"
                ? row.date
                  ? t("publishedOn", { date: formatDate(row.date, locale) })
                  : t("published")
                : t("earlier");
          return (
            <li key={i} className="flex items-baseline justify-between gap-3">
              <span className="text-ink-500">{label}</span>
              <span className="flex items-baseline gap-2 whitespace-nowrap">
                <span className="font-medium tabular-nums text-ink-900">{formatPrice(row.amount, row.currency)}</span>
                {percent && (
                  <span
                    className={
                      row.kind === "change" && row.changePercent < 0
                        ? "text-xs font-medium text-emerald-700"
                        : "text-xs font-medium text-accent-700"
                    }
                  >
                    {percent}
                  </span>
                )}
              </span>
            </li>
          );
        })}
      </ol>
      {rows.length > PRICE_HISTORY_PREVIEW_ROWS && (
        <button
          type="button"
          onClick={() => setExpanded((v) => !v)}
          aria-expanded={expanded}
          className="mt-2 text-sm font-medium text-brand-700 hover:text-brand-800"
        >
          {expanded ? t("showLess") : t("showAll", { count: rows.length })}
        </button>
      )}
    </section>
  );
}
