export type PropertyLocation = {
  country: string;
  region: string | null;
  city: string;
  district: string | null;
  sector: string | null;
  street: string | null;
  buildingNumber: string | null;
  latitude: number;
  longitude: number;
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
  location: PropertyLocation | null;
};
