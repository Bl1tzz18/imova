// Romanian diacritics mapped to their plain-Latin equivalent for typeahead matching — "î"/"â"
// both represent the same historical sound and are deliberately folded to the same letter here
// (not the more common î→i/â→a split some transliterations use), so a user typing either "Chis"
// or "Chisinau" without diacritics still matches "Chișinău".
const DIACRITIC_MAP: Record<string, string> = {
  ș: "s",
  ş: "s",
  ț: "t",
  ţ: "t",
  î: "a",
  â: "a",
  ă: "a",
};

export function normalizeForSearch(value: string): string {
  return Array.from(value.toLowerCase())
    .map((char) => DIACRITIC_MAP[char] ?? char)
    .join("");
}
