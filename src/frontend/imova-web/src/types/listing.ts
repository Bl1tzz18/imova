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

export type Photo = {
  id: string;
  listingId: string;
  url: string;
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
  furnishedStatus: string;
  availableFrom: string | null;
  petsAllowed: boolean;
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
  expiresAt: string | null;
  rejectionReason: string | null;
  suspensionReason: string | null;
  property: PropertyDetails;
  publisher: Publisher;
  photos: Photo[];
  isSaved: boolean;
};
