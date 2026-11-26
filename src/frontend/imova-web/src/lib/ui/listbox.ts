import { normalizeForSearch } from "@/lib/utils/search";

// The pure part of the site's list picker (components/ui/Select): moving through options with the
// keyboard, jumping by typed letters, and filtering long lists.

export type ListOption = { value: string; text: string; disabled?: boolean };

// Lists longer than this get a search field at the top (raion, sort-by-city, …).
export const SEARCH_THRESHOLD = 12;

// The next option that can be picked, `step` away from `from` (+1 down, -1 up), stopping at the
// ends. From -1 (nothing active) going down lands on the first one.
export function stepIndex(options: readonly ListOption[], from: number, step: 1 | -1): number {
  for (let i = from + step; i >= 0 && i < options.length; i += step) {
    if (!options[i].disabled) return i;
  }
  return from >= 0 && from < options.length ? from : edgeIndex(options, step === 1 ? "first" : "last");
}

export function edgeIndex(options: readonly ListOption[], edge: "first" | "last"): number {
  const order = options.map((_, i) => i);
  if (edge === "last") order.reverse();
  return order.find((i) => !options[i].disabled) ?? -1;
}

// Type-to-jump: the first option after `from` whose text starts with what was typed (diacritics
// ignored, wrapping round). Typing the same letter again moves on to the next option with it.
export function typeaheadIndex(options: readonly ListOption[], typed: string, from: number): number {
  const query = normalizeForSearch(typed);
  if (!query) return from;
  const repeated = [...query].every((c) => c === query[0]);
  const prefix = repeated ? query[0] : query;
  const start = repeated || from < 0 ? from + 1 : from;
  for (let k = 0; k < options.length; k++) {
    const i = (start + k + options.length) % options.length;
    const option = options[i];
    if (!option.disabled && normalizeForSearch(option.text).startsWith(prefix)) return i;
  }
  return from;
}

export function filterOptions<T extends ListOption>(options: readonly T[], query: string): T[] {
  const q = normalizeForSearch(query.trim());
  return q ? options.filter((o) => normalizeForSearch(o.text).includes(q)) : [...options];
}
