type Property = {
  id: string;
  title: string;
  price: number;
  currency: string;
  city: string;
  district: string;
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
      <p>Anunturi primite din backend (Imova.Api):</p>
      <ul>
        {properties.map((property) => (
          <li key={property.id}>
            {property.title} — {property.price} {property.currency} (
            {property.city}, {property.district})
          </li>
        ))}
      </ul>
    </main>
  );
}
