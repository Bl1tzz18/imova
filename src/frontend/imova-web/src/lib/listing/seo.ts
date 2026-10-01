import type { Listing } from "@/types/listing";

// What a listing looks like outside the site — the link preview in Viber/Telegram/Facebook
// (description, image) and Google's structured data (JSON-LD). Pure (Vitest-covered); the page
// passes in the translated words and the site's own URL.

const DESCRIPTION_LENGTH = 160;

export function listingPath(id: string): string {
  return `/property/${id}`;
}

// The cover photo: the one marked primary, else the first.
export function coverPhotoUrl(listing: Pick<Listing, "photos">): string | null {
  return (listing.photos.find((p) => p.isPrimary) ?? listing.photos[0])?.url ?? null;
}

// Cut on a word boundary with an ellipsis, whitespace collapsed (descriptions have line breaks).
export function truncateText(text: string, max: number): string {
  const flat = text.replace(/\s+/g, " ").trim();
  if (flat.length <= max) return flat;
  const cut = flat.slice(0, max - 1);
  const lastSpace = cut.lastIndexOf(" ");
  return `${(lastSpace > max * 0.6 ? cut.slice(0, lastSpace) : cut).replace(/[\s,.;:–-]+$/, "")}…`;
}

// "82 000 € · Apartament · 62 m² · Chișinău, Botanica. Apartament luminos cu 2 camere…" — the facts
// a preview needs first, then the owner's own words to fill the rest.
export function listingMetaDescription(facts: (string | null)[], description: string): string {
  const head = facts.filter((f): f is string => Boolean(f)).join(" · ");
  const room = DESCRIPTION_LENGTH - head.length - 2;
  return room > 30 && description.trim() ? `${head}. ${truncateText(description, room)}` : truncateText(head, DESCRIPTION_LENGTH);
}

// schema.org RealEstateListing with its Offer and the place itself. Address only as precise as the
// page shows it; coordinates and rooms when known.
export function listingJsonLd(listing: Listing, url: string): Record<string, unknown> {
  const location = listing.property.location;
  const rooms = (listing.property.typeSpecificAttributes as Record<string, unknown> | null)?.rooms;
  const place: Record<string, unknown> = {
    "@type": "Place",
    floorSize: { "@type": "QuantitativeValue", value: listing.property.totalAreaM2, unitCode: "MTK" },
  };
  if (typeof rooms === "number") place.numberOfRooms = rooms;
  if (location) {
    place.address = {
      "@type": "PostalAddress",
      addressCountry: "MD",
      addressRegion: location.raionName,
      ...(location.localitateName || location.chisinauSectorName
        ? { addressLocality: location.localitateName ?? location.chisinauSectorName }
        : {}),
      ...(location.street
        ? { streetAddress: [location.street, location.buildingNumber].filter(Boolean).join(" ") }
        : {}),
    };
    if (location.latitude != null && location.longitude != null) {
      place.geo = { "@type": "GeoCoordinates", latitude: location.latitude, longitude: location.longitude };
    }
  }

  return {
    "@context": "https://schema.org",
    "@type": "RealEstateListing",
    name: listing.title,
    description: truncateText(listing.description, 500),
    url,
    ...(listing.publishedAt ? { datePosted: listing.publishedAt } : {}),
    ...(listing.photos.length > 0 ? { image: listing.photos.map((p) => p.url) } : {}),
    offers: {
      "@type": "Offer",
      price: listing.price.amount,
      priceCurrency: listing.price.currency,
      availability: "https://schema.org/InStock",
      businessFunction: listing.transactionType === "Rent"
        ? "http://purl.org/goodrelations/v1#LeaseOut"
        : "http://purl.org/goodrelations/v1#Sell",
    },
    about: place,
  };
}

// JSON for a <script type="application/ld+json">: "<" escaped so text like "</script>" in a title
// can't end the script element early.
export function jsonLdScript(data: Record<string, unknown>): string {
  return JSON.stringify(data).replace(/</g, "\\u003c");
}
