// Page numbers to show: the first, the last, and a window around the current one, with null for a
// gap ("…"). E.g. current 6 of 12 → [1, null, 5, 6, 7, null, 12].
export function pageWindow(current: number, total: number, radius = 1): (number | null)[] {
  if (total <= 1) return [];
  const pages = new Set([1, total]);
  for (let p = current - radius; p <= current + radius; p++) {
    if (p >= 1 && p <= total) pages.add(p);
  }
  const sorted = [...pages].sort((a, b) => a - b);
  const result: (number | null)[] = [];
  sorted.forEach((page, i) => {
    if (i > 0 && page - sorted[i - 1] > 1) result.push(null);
    result.push(page);
  });
  return result;
}
