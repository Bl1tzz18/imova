// A listing's address: /property/100015-apartament-2-camere-botanica — the short public number
// (what finds the listing) and its title in plain Latin letters (what people and search engines
// read). Only the number counts; the words after it are rebuilt from the current title, and the
// page redirects any other words (an old title, a cut-off link) to them. Old links by id
// (/property/<guid>) keep working the same way. Pure (Vitest-covered).

const MAX_SLUG_LENGTH = 60;

// Romanian letters with diacritics and Russian Cyrillic in Latin letters (a simple, readable
// transliteration — the one people type in searches: "kvartira", "botanika").
const LATIN: Record<string, string> = {
  ă: "a", â: "a", î: "i", ș: "s", ş: "s", ț: "t", ţ: "t",
  а: "a", б: "b", в: "v", г: "g", д: "d", е: "e", ё: "e", ж: "zh", з: "z", и: "i", й: "i", к: "k",
  л: "l", м: "m", н: "n", о: "o", п: "p", р: "r", с: "s", т: "t", у: "u", ф: "f", х: "h", ц: "ts",
  ч: "ch", ш: "sh", щ: "sch", ъ: "", ы: "y", ь: "", э: "e", ю: "iu", я: "ia",
};

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

// "Apartament cu 2 camere, bloc nou, Botanica" → "apartament-cu-2-camere-bloc-nou-botanica";
// cut on a word boundary at MAX_SLUG_LENGTH.
export function slugify(text: string): string {
  const latin = [...text.toLowerCase()]
    .map((ch) => LATIN[ch] ?? ch)
    .join("")
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "");
  const slug = latin.replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "");
  if (slug.length <= MAX_SLUG_LENGTH) return slug;
  const cut = slug.slice(0, MAX_SLUG_LENGTH + 1);
  const lastDash = cut.lastIndexOf("-");
  return (lastDash > 0 ? cut.slice(0, lastDash) : slug.slice(0, MAX_SLUG_LENGTH)).replace(/-+$/, "");
}

// The path segment after /property/: "100015-apartament-…", or just the number when the title has
// no letters or digits to keep.
export function listingSlug(number: number, title: string): string {
  const words = slugify(title);
  return words ? `${number}-${words}` : String(number);
}

// The listing's address. Prefer passing the number and title (every listing on a public page has
// them); with only an id the link still works — the page redirects it to the address above.
export function listingPath(listing: { id: string; number?: number | null; title?: string | null }): string {
  return listing.number != null
    ? `/property/${listingSlug(listing.number, listing.title ?? "")}`
    : `/property/${listing.id}`;
}

export type ListingSlugRef = { kind: "number"; number: number } | { kind: "id"; id: string };

// What a /property/[slug] segment points at: a number (with any words after it) or a legacy id.
export function parseListingSlug(slug: string): ListingSlugRef | null {
  const decoded = safeDecode(slug);
  if (GUID.test(decoded)) return { kind: "id", id: decoded.toLowerCase() };
  const match = /^(\d{1,15})(?:-|$)/.exec(decoded);
  return match ? { kind: "number", number: Number(match[1]) } : null;
}

function safeDecode(segment: string): string {
  try {
    return decodeURIComponent(segment);
  } catch {
    return segment;
  }
}
