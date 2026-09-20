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

export function formatFullLocation(location: {
  country: string;
  region: string | null;
  raionName: string;
  localitateName: string | null;
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
    location.localitateName,
    location.raionName,
    location.region,
    location.country,
  ].filter(Boolean);
  return parts.join(", ");
}

export function formatLocation(location: {
  raionName: string;
  localitateName: string | null;
} | null) {
  if (!location) return null;
  return location.localitateName ? `${location.raionName}, ${location.localitateName}` : location.raionName;
}
