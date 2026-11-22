import { searchHref, type SearchState } from "./filters";

// Which /search pages search engines should index. Only the ones people actually search for —
// a transaction, one property type, a place — on their first page. Every other combination (a
// price range, a sort, page 7, a dozen amenities) is the same listings in another order or a
// sliver of them: such pages stay out of the index ("noindex, follow" — the listings they link
// to still get found). Pure (Vitest-covered).
const INDEXED_PARAMS = new Set(["transactionType", "propertyType", "raionId", "localitateId", "chisinauSectorId"]);

export function searchIndexing(state: SearchState): { indexable: true; path: string } | { indexable: false } {
  const indexable = Object.keys(state).every((key) => INDEXED_PARAMS.has(key));
  return indexable ? { indexable: true, path: searchHref(state) } : { indexable: false };
}
