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

export type PropertyOwner = {
  email: string;
  phone: string | null;
};

export type PropertyMedia = {
  id: string;
  propertyId: string;
  url: string;
  contentType: string;
  fileSizeBytes: number;
  moderationStatus: string;
  sortOrder: number;
  createdAt: string;
};

export type Property = {
  id: string;
  ownerId: string;
  organizationId: string | null;
  title: string;
  description: string;
  propertyType: string;
  listingType: string;
  status: string;
  price: number;
  currency: string;
  area: number | null;
  rooms: number | null;
  bathrooms: number | null;
  floor: number | null;
  totalFloors: number | null;
  yearBuilt: number | null;
  furnished: boolean | null;
  parkingAvailable: boolean | null;
  petsAllowed: boolean | null;
  createdAt: string;
  updatedAt: string;
  publishedAt: string | null;
  expiresAt: string | null;
  rejectionReason: string | null;
  suspensionReason: string | null;
  location: PropertyLocation | null;
  owner: PropertyOwner | null;
  media: PropertyMedia[];
  isSaved: boolean;
};
