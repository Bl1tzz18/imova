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
  title: string;
  description: string;
  propertyType: string;
  listingType: string;
  status: string;
  price: number;
  currency: string;
  area: number | null;
  rooms: number | null;
  location: PropertyLocation | null;
};
