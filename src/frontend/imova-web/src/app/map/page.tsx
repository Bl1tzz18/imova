import { PropertyMapExplorer } from "@/components/property/PropertyMapExplorer";
import type { Property } from "@/types/property";

async function getProperties(): Promise<Property[]> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/properties`, { cache: "no-store" });

  if (!res.ok) {
    throw new Error(`Failed to fetch properties: ${res.status}`);
  }

  return res.json();
}

export default async function HartaPage() {
  const properties = await getProperties();

  return <PropertyMapExplorer properties={properties} />;
}
