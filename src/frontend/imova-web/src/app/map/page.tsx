import { Footer } from "@/components/layout/Footer";
import { PropertyMapExplorer } from "@/components/property/PropertyMapExplorer";
import { getSessionToken } from "@/lib/auth/session";
import type { Property } from "@/types/property";

async function getProperties(): Promise<Property[]> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  const res = await fetch(`${apiUrl}/api/v1/properties`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch properties: ${res.status}`);
  }

  return res.json();
}

export default async function HartaPage() {
  const properties = await getProperties();

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <PropertyMapExplorer properties={properties} />
      </main>
      <Footer />
    </div>
  );
}
