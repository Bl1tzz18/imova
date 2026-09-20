// Romanian diacritics mapped to their plain-Latin equivalent for typeahead matching. ș/ț are
// unambiguous (always s/t). "a"/"ă"/"â"/"î"/"i" are deliberately folded into ONE bucket rather
// than split by the "correct" transliteration (î→i, â/ă→a) — real place names swap between the
// old î-everywhere Moldovan spelling and the current â-mid-word one (e.g. official CUATM data has
// both "Rîșcani" and "Râșcani" in the wild), and people who grew up typing one convention often
// type the other without diacritics. Treating all five as interchangeable means "Riscani",
// "Rascani", "Râșcani" and "Rîșcani" all normalize the same way, so nobody's typing habit is a
// dead end — a deliberate accessibility tradeoff over stricter/"more correct" matching.
const DIACRITIC_MAP: Record<string, string> = {
  ș: "s",
  ş: "s",
  ț: "t",
  ţ: "t",
  ă: "a",
  â: "a",
  î: "a",
  i: "a",
};

export function normalizeForSearch(value: string): string {
  return Array.from(value.toLowerCase())
    .map((char) => DIACRITIC_MAP[char] ?? char)
    .join("");
}
