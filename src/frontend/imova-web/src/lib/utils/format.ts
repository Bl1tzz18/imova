// Always formatted with a space thousands separator (e.g. "100 000 €"), regardless of
// the active site language — this is the format the user asked to see everywhere.
export function formatPrice(price: number, currency: string) {
  return new Intl.NumberFormat("ru-RU", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(price);
}

export function formatPricePerArea(price: number, area: number, currency: string) {
  return `${formatPrice(Math.round(price / area), currency)}/m²`;
}

export function formatDate(isoDate: string, locale: string) {
  return new Intl.DateTimeFormat(locale, { dateStyle: "long" }).format(new Date(isoDate));
}

export function formatFullLocation(location: {
  country: string;
  region: string | null;
  city: string;
  district: string | null;
  sector: string | null;
  street: string | null;
  buildingNumber: string | null;
} | null) {
  if (!location) return null;
  const parts = [
    location.street
      ? `${location.street}${location.buildingNumber ? ` ${location.buildingNumber}` : ""}`
      : null,
    location.sector,
    location.district,
    location.city,
    location.region,
    location.country,
  ].filter(Boolean);
  return parts.join(", ");
}

export function formatLocation(location: {
  city: string;
  district: string | null;
} | null) {
  if (!location) return null;
  return location.district ? `${location.city}, ${location.district}` : location.city;
}
