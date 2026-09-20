// Always formatted with a space thousands separator (e.g. "100 000 €"), regardless of
// the active site language — this is the format the user asked to see everywhere.
export function formatPrice(price: number, currency: string) {
  return new Intl.NumberFormat("ru-RU", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(price);
}

export function formatDate(isoDate: string, locale: string) {
  return new Intl.DateTimeFormat(locale, { dateStyle: "long" }).format(new Date(isoDate));
}

// Several raion seats share their raion's exact name (e.g. the town "Soroca" is the seat of
// Soroca raion) — showing both would read as "Soroca, Soroca", so the raion name is dropped
// whenever it's identical to the more specific localitate/sector name.
function sameName(a: string | null, b: string | null) {
  return a != null && b != null && a.trim() === b.trim();
}

export function formatFullLocation(location: {
  country: string;
  region: string | null;
  raionName: string;
  localitateName: string | null;
  chisinauSectorName: string | null;
  sector: string | null;
  street: string | null;
  buildingNumber: string | null;
} | null) {
  if (!location) return null;
  const raionName = sameName(location.raionName, location.localitateName) ? null : location.raionName;
  const parts = [
    location.street
      ? `${location.street}${location.buildingNumber ? ` ${location.buildingNumber}` : ""}`
      : null,
    location.chisinauSectorName,
    location.sector,
    location.localitateName,
    raionName,
    location.region,
    location.country,
  ].filter(Boolean);
  return parts.join(", ");
}

// Suburb (localitateName) and informal Chișinău neighborhood (chisinauSectorName) are mutually
// exclusive — a listing is never in both at once — so at most one of them is ever non-null here;
// this still just includes whichever is present rather than special-casing "which one", plus the
// same raion-seat-town dedup formatFullLocation uses (e.g. avoids "Soroca, Soroca").
export function formatLocation(location: {
  raionName: string;
  localitateName: string | null;
  chisinauSectorName: string | null;
} | null) {
  if (!location) return null;
  const specifics = [location.chisinauSectorName, location.localitateName].filter(
    (name): name is string => Boolean(name) && !sameName(location.raionName, name),
  );
  return specifics.length > 0 ? `${location.raionName}, ${specifics.join(", ")}` : location.raionName;
}
