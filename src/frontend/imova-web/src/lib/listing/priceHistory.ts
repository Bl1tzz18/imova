import type { PriceHistory } from "@/types/listing";

// One line of a listing's price history, newest first: what the price became and when, with the
// change in percent; the last line is where it started — at publication, or (when the API left
// older changes out) just "earlier", without a date.
export type PriceHistoryRow =
  | { kind: "change"; amount: number; currency: string; date: string; changePercent: number }
  | { kind: "published"; amount: number; currency: string; date: string | null }
  | { kind: "earlier"; amount: number; currency: string };

export function priceHistoryRows(history: PriceHistory, publishedAt: string | null): PriceHistoryRow[] {
  if (history.changes.length === 0) return [];
  const rows: PriceHistoryRow[] = [...history.changes].reverse().map((c) => ({
    kind: "change",
    amount: c.newAmount,
    currency: c.newCurrency,
    date: c.changedAt,
    changePercent: c.changePercent,
  }));
  const first = history.changes[0];
  rows.push(
    history.startsAtPublication
      ? { kind: "published", amount: first.oldAmount, currency: first.oldCurrency, date: publishedAt }
      : { kind: "earlier", amount: first.oldAmount, currency: first.oldCurrency },
  );
  return rows;
}

// "−3%" / "+2%" with a real minus sign; a change that rounds to nothing shows no percent at all.
export function formatPercentChange(percent: number): string | null {
  if (percent === 0) return null;
  return percent < 0 ? `−${Math.abs(percent)}%` : `+${percent}%`;
}

// The history list shows this many lines before "Show all".
export const PRICE_HISTORY_PREVIEW_ROWS = 4;
