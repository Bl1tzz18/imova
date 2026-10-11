"use client";

import { useId, useState } from "react";
import { useTranslations } from "next-intl";
import { axisDayIndexes, niceMax } from "@/lib/agency/stats";
import { cn } from "@/lib/utils/cn";

type Point = { day: string; value: number };

// "3 300" — the same grouping as the tiles above.
const number = new Intl.NumberFormat("ru-RU");

// "9 oct." — a date-only day from the API, so it's read and written in UTC (no shift).
function dayLabel(day: string, locale: string, long = false): string {
  return new Intl.DateTimeFormat(locale, {
    day: "numeric",
    month: long ? "long" : "short",
    timeZone: "UTC",
  }).format(new Date(`${day}T00:00:00Z`));
}

// One measure per day as thin bars (single series: the title names it, no legend). Hover, a tap or
// the arrow keys show that day's number; the same numbers are in a table under "Vezi tabelul".
export function DailyBarChart({
  title,
  points,
  locale,
  barClassName,
}: {
  title: string;
  points: Point[];
  locale: string;
  barClassName: string;
}) {
  const t = useTranslations("AgencyStats");
  const [active, setActive] = useState<number | null>(null);
  const tableId = useId();
  const max = niceMax(points.map((p) => p.value));
  const total = points.reduce((sum, p) => sum + p.value, 0);
  const dense = points.length > 31;
  const shown = active !== null ? points[active] : null;

  function pick(clientX: number, rect: DOMRect) {
    if (points.length === 0) return;
    const index = Math.floor(((clientX - rect.left) / rect.width) * points.length);
    setActive(Math.min(points.length - 1, Math.max(0, index)));
  }

  function onKeyDown(e: React.KeyboardEvent) {
    if (points.length === 0) return;
    if (e.key === "ArrowRight" || e.key === "ArrowLeft") {
      e.preventDefault();
      const step = e.key === "ArrowRight" ? 1 : -1;
      setActive((i) => Math.min(points.length - 1, Math.max(0, (i ?? (step > 0 ? -1 : points.length)) + step)));
    } else if (e.key === "Escape") {
      setActive(null);
    }
  }

  return (
    <section className="rounded-2xl border border-line bg-white p-4 sm:p-5">
      <div className="flex items-baseline justify-between gap-3">
        <h3 className="text-sm font-semibold text-ink-900">{title}</h3>
        <p className="text-sm text-ink-500 tabular-nums">{t("chartTotal", { count: number.format(total) })}</p>
      </div>

      {/* The day under the pointer — a fixed line, so the chart doesn't jump. */}
      <p className="mt-1 h-5 text-sm text-ink-700 tabular-nums" aria-live="polite">
        {shown ? t("chartDay", { day: dayLabel(shown.day, locale, true), count: shown.value }) : ""}
      </p>

      <div className="mt-2 flex gap-2">
        {/* The scale: its top and zero, recessive. */}
        <div className="flex h-36 flex-col justify-between text-right text-[11px] leading-none text-ink-400 tabular-nums" aria-hidden>
          <span>{max}</span>
          <span>0</span>
        </div>

        <div className="min-w-0 flex-1">
          <div
            role="img"
            aria-label={t("chartLabel", { title, count: number.format(total) })}
            aria-describedby={tableId}
            tabIndex={0}
            onPointerMove={(e) => pick(e.clientX, e.currentTarget.getBoundingClientRect())}
            onPointerDown={(e) => pick(e.clientX, e.currentTarget.getBoundingClientRect())}
            onPointerLeave={(e) => e.pointerType === "mouse" && setActive(null)}
            onKeyDown={onKeyDown}
            onBlur={() => setActive(null)}
            className={cn(
              "relative flex h-36 touch-pan-y items-end border-b border-ink-200 outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2",
              dense ? "gap-px" : "gap-[2px]",
            )}
          >
            <div className="pointer-events-none absolute inset-x-0 top-0 border-t border-dashed border-ink-100" aria-hidden />
            {points.map((p, i) => (
              <div key={p.day} className="flex h-full min-w-0 flex-1 items-end" aria-hidden>
                <div
                  className={cn(
                    "w-full rounded-t-[4px] transition-opacity",
                    barClassName,
                    active !== null && active !== i && "opacity-40",
                  )}
                  style={{ height: p.value === 0 ? 0 : `max(2px, ${(p.value / max) * 100}%)` }}
                />
              </div>
            ))}
          </div>

          <div className="relative mt-1.5 h-4 text-[11px] text-ink-400" aria-hidden>
            {axisDayIndexes(points.length).map((i, n, all) => (
              <span
                key={i}
                className={cn(
                  "absolute whitespace-nowrap",
                  n === 0 ? "left-0" : n === all.length - 1 ? "right-0" : "-translate-x-1/2",
                )}
                style={n > 0 && n < all.length - 1 ? { left: `${((i + 0.5) / points.length) * 100}%` } : undefined}
              >
                {dayLabel(points[i].day, locale)}
              </span>
            ))}
          </div>
        </div>
      </div>

      <details className="mt-3 text-sm">
        <summary className="cursor-pointer font-medium text-brand-700 hover:underline">{t("showTable")}</summary>
        <div className="mt-2 max-h-64 overflow-y-auto rounded-xl border border-line">
          <table id={tableId} className="w-full text-left text-sm">
            <caption className="sr-only">{title}</caption>
            <thead className="sticky top-0 bg-ink-50 text-ink-500">
              <tr>
                <th scope="col" className="px-3 py-2 font-medium">{t("tableDay")}</th>
                <th scope="col" className="px-3 py-2 text-right font-medium">{title}</th>
              </tr>
            </thead>
            <tbody>
              {points.map((p) => (
                <tr key={p.day} className="border-t border-line">
                  <td className="px-3 py-1.5 text-ink-700">{dayLabel(p.day, locale, true)}</td>
                  <td className="px-3 py-1.5 text-right text-ink-900 tabular-nums">{p.value}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </details>
    </section>
  );
}
