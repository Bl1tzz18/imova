// Mirrors Imova.Contracts.Listings.ListingDto and the DTOs it nests. A Listing is the published
// offer; `property` is the physical asset behind it (shared by every listing of that asset).

export type PropertyLocation = {
  country: string;
  region: string | null;
  raionId: string;
  raionName: string;
  localitateId: string | null;
  localitateName: string | null;
  chisinauSectorId: string | null;
  chisinauSectorName: string | null;
  sector: string | null;
  street: string | null;
  buildingNumber: string | null;
  latitude: number | null;
  longitude: number | null;
};

export type Amenity = {
  id: string;
  key: string;
  labelRo: string;
  // "General" | "Comfort" | "Security" | "Leisure" — groups amenities for display.
  category: string;
  // PropertyType names the amenity can be selected for.
  applicablePropertyTypes: string[];
};

// Something a property is close to (school, park, ...) — separate from amenities, which describe
// the property itself.
export type Proximity = {
  id: string;
  key: string;
  labelRo: string;
  // PropertyType names the proximity can be selected for.
  applicablePropertyTypes: string[];
};

// Who to contact about a listing (detail view only). Name/email are already resolved — a "Self"
// contact carries the publisher's own. phone is null when the owner hid it (unless the viewer is
// the owner or an admin).
export type ListingContact = {
  personType: "Self" | "Other";
  name: string | null;
  phone: string | null;
  email: string | null;
  messagingApps: string[];
  preferredContactMethod: "PhoneCall" | "PlatformMessages" | "Any";
  hidePhoneNumber: boolean;
  // "HH:mm", both or neither — e.g. 09:00 and 18:00.
  callHoursFrom: string | null;
  callHoursTo: string | null;
  // The person's photo (Self: the publisher's profile picture, else the agency's logo); null for
  // an "Other" person.
  pictureUrl: string | null;
  // Set when an agency publishes the listing — the contact works for it.
  agencyName: string | null;
  // What the public may know of the number before asking for it (POST …/contact/phone): its first
  // digits and how many follow. `phone` itself is only there for the owner or an admin. Both null
  // without a number or when the owner hid it.
  phonePrefix: string | null;
  phoneHiddenDigits: number | null;
};

export type PublisherType = "Individual" | "Agency";

// phone/email are null wherever contact details aren't exposed (cards/search results) — only a
// listing's detail view and the "my publishers" list carry them.
export type Publisher = {
  id: string;
  userId: string;
  publisherType: PublisherType;
  displayName: string;
  phone: string | null;
  email: string | null;
  logoUrl: string | null;
  bio: string | null;
};

// "Preț redus": the price is down `percent` from previousAmount since reducedAt (rule: the API's
// ListingPriceHistory).
export type PriceReduction = {
  previousAmount: number;
  previousCurrency: string;
  percent: number;
  reducedAt: string;
};

// changePercent is signed (negative = cheaper).
export type PriceChange = {
  oldAmount: number;
  oldCurrency: string;
  newAmount: number;
  newCurrency: string;
  changePercent: number;
  changedAt: string;
};

// Oldest first; startsAtPublication: changes[0].oldAmount is the price it was published at.
export type PriceHistory = {
  startsAtPublication: boolean;
  changes: PriceChange[];
};

// url is the large display size (≤1600px), cardUrl ≤800px, thumbnailUrl ≤400px — JPEGs made from the
// upload. Until they exist (briefly, after an upload whose sizes failed) all three are the original.
export type Photo = {
  id: string;
  listingId: string;
  url: string;
  thumbnailUrl: string;
  cardUrl: string;
  contentType: string;
  fileSizeBytes: number;
  moderationStatus: string;
  sortOrder: number;
  isPrimary: boolean;
  createdAt: string;
};

// Shape depends on propertyType — see ATTRIBUTE_SCHEMA in lib/property/attributeSchema.ts.
export type TypeSpecificAttributes = Record<string, unknown>;

export type PropertyDetails = {
  id: string;
  propertyType: string;
  totalAreaM2: number;
  yearBuilt: number | null;
  condition: string | null;
  typeSpecificAttributes: TypeSpecificAttributes;
  amenities: Amenity[];
  proximities: Proximity[];
  location: PropertyLocation | null;
};

export type Price = {
  amount: number;
  currency: string;
  // The amount converted to EUR at save time — what price filtering/sorting compares.
  priceEur: number;
  isNegotiable: boolean;
};

export type RentalDetails = {
  minLeasePeriodMonths: number | null;
  securityDepositAmount: number | null;
  utilitiesIncluded: boolean;
  availableFrom: string | null;
  // Null when not asked (rentals other than an apartment, house or room).
  petsAllowed: boolean | null;
};

export type SaleDetails = {
  ownershipDocumentType: string | null;
};

export type ListingStatus =
  | "Draft"
  | "PendingReview"
  | "Active"
  | "Rejected"
  | "Suspended"
  | "Expired"
  | "Sold"
  | "Rented"
  | "Archived";

export type Listing = {
  id: string;
  status: ListingStatus;
  transactionType: "Sale" | "Rent";
  title: string;
  description: string;
  price: Price;
  saleDetails: SaleDetails | null;
  rentalDetails: RentalDetails | null;
  createdAt: string;
  updatedAt: string;
  publishedAt: string | null;
  // The short public number ("ID 100231").
  number: number;
  // Different people who opened it / asked for its phone number, each counted once a day — never
  // the owner. The owner's and admins' only, else null.
  viewCount: number | null;
  phoneRevealCount: number | null;
  expiresAt: string | null;
  rejectionReason: string | null;
  suspensionReason: string | null;
  property: PropertyDetails;
  publisher: Publisher;
  photos: Photo[];
  isSaved: boolean;
  // Only on the detail view — null/absent on cards and search results.
  contact?: ListingContact | null;
  // On every view while the price is down (cards show it too).
  priceReduction?: PriceReduction | null;
  // Only on the detail view; null when the price never changed since publication.
  priceHistory?: PriceHistory | null;
};

// What may still be shown of a listing that has ended (GET /api/v1/listings/{id} → 410 Gone): no
// photos, description, address or contact.
export type EndedListingStatus = "Sold" | "Rented" | "Expired" | "Archived";

export type EndedListing = {
  id: string;
  status: EndedListingStatus;
  transactionType: "Sale" | "Rent";
  propertyType: string;
  title: string;
  price: Listing["price"];
  totalAreaM2: number;
  raionId: string;
  raionName: string;
  localitateName: string | null;
  chisinauSectorName: string | null;
  endedAt: string;
};
