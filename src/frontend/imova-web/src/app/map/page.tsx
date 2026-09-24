import { Footer } from "@/components/layout/Footer";
import { PropertyMapExplorer } from "@/components/property/PropertyMapExplorer";
import { getSessionToken } from "@/lib/auth/session";
import type { Listing } from "@/types/listing";

async function getListings(): Promise<Listing[]> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  const res = await fetch(`${apiUrl}/api/v1/listings`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch listings: ${res.status}`);
  }

  return res.json();
}

export default async function HartaPage() {
  const listings = await getListings();

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <PropertyMapExplorer listings={listings} />
      </main>
      <Footer />
    </div>
  );
}
