import Link from "next/link";

type PropertyLocation = {
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

type Property = {
  id: string;
  ownerId: string;
  title: string;
  description: string;
  propertyType: string;
  listingType: string;
  status: string;
  price: number;
  currency: string;
  location: PropertyLocation | null;
};

async function getProperties(): Promise<Property[]> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/properties`, { cache: "no-store" });

  if (!res.ok) {
    throw new Error(`Failed to fetch properties: ${res.status}`);
  }

  return res.json();
}

export default async function Home() {
  const properties = await getProperties();

  return (
    <main style={{ fontFamily: "sans-serif", padding: "2rem" }}>
      <h1>IMOVA</h1>
      <p>
        Anunturi primite din backend (Imova.Api): <Link href="/properties/new">Adaugă anunț</Link>
      </p>
      <ul>
        {properties.map((property) => (
          <li key={property.id} style={{ marginBottom: "0.75rem" }}>
            <strong>{property.title}</strong> — {property.price} {property.currency}
            {" "}
            ({property.listingType === "Rent" ? "chirie" : "vânzare"})
            <br />
            {property.propertyType} · {property.status}
            {property.location && (
              <>
                {" "}
                · {property.location.city}
                {property.location.district ? `, ${property.location.district}` : ""}
              </>
            )}
          </li>
        ))}
      </ul>
    </main>
  );
}
