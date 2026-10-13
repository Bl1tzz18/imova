// The Statistici tab — pure helpers (Vitest-covered): the period presets in the address (?days=),
// the API query, the change against the period before, and the bar chart's scale.

export const STATS_PRESETS = [7, 30, 90] as const;
type StatsPreset = (typeof STATS_PRESETS)[number];
const DEFAULT_STATS_PRESET: StatsPreset = 30;

type Params = Record<string, string | string[] | undefined>;

export function parseStatsPreset(params: Params): StatsPreset {
  const raw = Array.isArray(params.days) ? params.days[0] : params.days;
  const days = Number(raw);
  return (STATS_PRESETS as readonly number[]).includes(days) ? (days as StatsPreset) : DEFAULT_STATS_PRESET;
}

// The last `days` Moldovan days ending `today` (yyyy-MM-dd, as the API reads them).
export function statsApiQuery(days: StatsPreset, today: string): string {
  const to = new Date(`${today}T00:00:00Z`);
  const from = new Date(to.getTime() - (days - 1) * 86_400_000);
  return `from=${from.toISOString().slice(0, 10)}&to=${today}`;
}

// Today in Moldova as yyyy-MM-dd (the en-CA format is ISO-like).
export function moldovaToday(now: Date): string {
  return new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Chisinau", year: "numeric", month: "2-digit", day: "2-digit" }).format(now);
}

export type Change = { kind: "new" } | { kind: "same" } | { kind: "up" | "down"; percent: number };

// Against the period before: up/down by a whole percent; "new" when there was nothing before.
export function change(current: number, previous: number): Change {
  if (previous === 0) return current === 0 ? { kind: "same" } : { kind: "new" };
  const percent = Math.round(((current - previous) / previous) * 100);
  if (percent === 0) return { kind: "same" };
  return { kind: percent > 0 ? "up" : "down", percent: Math.abs(percent) };
}

// How many of the views turned into a contact, as a percent with one decimal ("2.4"); null without views.
export function contactRate(contacts: number, views: number): number | null {
  if (views <= 0) return null;
  return Math.round((contacts / views) * 1000) / 10;
}

// What fixes a listing in "De îmbunătățit": its photos step, renewing it, or a fresh look at the
// listing (price, description, photos) when it's seen but nobody gets in touch.
type AttentionReason = "FewPhotos" | "NoContacts" | "ExpiringSoon";
type AttentionFix = { kind: "renew" } | { kind: "edit"; href: string };

export function attentionFix(reason: AttentionReason, listingId: string): AttentionFix {
  if (reason === "ExpiringSoon") return { kind: "renew" };
  const edit = `/my-listings/${listingId}/edit`;
  return { kind: "edit", href: reason === "FewPhotos" ? `${edit}?step=photos` : edit };
}

// The days named under the chart: the first, the middle and the last (fewer for a short period).
export function axisDayIndexes(count: number): number[] {
  if (count <= 0) return [];
  if (count <= 2) return Array.from({ length: count }, (_, i) => i);
  return [0, Math.floor((count - 1) / 2), count - 1];
}

// A round top for the chart's scale (1, 2, 5 × 10ⁿ), never below 1 — so an empty period still draws.
export function niceMax(values: number[]): number {
  const max = Math.max(1, ...values);
  const power = 10 ** Math.floor(Math.log10(max));
  return [1, 2, 5, 10].map((m) => m * power).find((v) => v >= max) ?? 10 * power;
}
