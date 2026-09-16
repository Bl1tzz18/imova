export function formatPrice(price: number, currency: string, locale: string) {
  return new Intl.NumberFormat(locale, {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(price);
}

export function formatLocation(location: {
  city: string;
  district: string | null;
} | null) {
  if (!location) return null;
  return location.district ? `${location.city}, ${location.district}` : location.city;
}
