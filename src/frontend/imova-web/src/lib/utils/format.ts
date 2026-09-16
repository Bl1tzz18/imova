export function formatPrice(price: number, currency: string, locale: string) {
  return new Intl.NumberFormat(locale, {
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
  city: string;
  district: string | null;
  street: string | null;
  buildingNumber: string | null;
} | null) {
  if (!location) return null;
  const parts = [
    location.street
      ? `${location.street}${location.buildingNumber ? ` ${location.buildingNumber}` : ""}`
      : null,
    location.district,
    location.city,
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
