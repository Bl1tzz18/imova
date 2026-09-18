import { redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { PropertyCard } from "@/components/property/PropertyCard";
import { LinkButton } from "@/components/ui/Button";
import { getSessionToken } from "@/lib/auth/session";
import type { Property } from "@/types/property";

async function getSavedListings(token: string): Promise<Property[]> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/users/me/favorites`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch saved listings: ${res.status}`);
  }

  return res.json();
}

export default async function SavedListingsPage() {
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/saved-listings");
  }

  const [properties, t] = await Promise.all([
    getSavedListings(token),
    getTranslations("SavedListingsPage"),
  ]);

  return (
    <main className="mx-auto max-w-6xl px-4 py-10 sm:px-6">
      <h1 className="font-display text-2xl font-medium text-ink-950 sm:text-3xl">{t("title")}</h1>
      <p className="mt-1 text-sm text-ink-500">
        {properties.length > 0 ? t("resultsCount", { count: properties.length }) : t("emptyTitle")}
      </p>

      {properties.length > 0 ? (
        <div className="mt-8 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {properties.map((property) => (
            <PropertyCard key={property.id} property={property} />
          ))}
        </div>
      ) : (
        <div className="mt-8 flex flex-col items-center gap-4 rounded-2xl border border-dashed border-ink-200 bg-white px-6 py-16 text-center">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.4" className="h-10 w-10 text-ink-300">
            <path d="M12 21s-7.5-4.6-10-9.3C.5 8.1 2.4 4.5 6 4c2.1-.3 4 .8 6 3 2-2.2 3.9-3.3 6-3 3.6.5 5.5 4.1 4 7.7C19.5 16.4 12 21 12 21Z" strokeLinejoin="round" />
          </svg>
          <p className="text-sm text-ink-500">{t("emptyBody")}</p>
          <LinkButton href="/search">{t("browseListings")}</LinkButton>
        </div>
      )}
    </main>
  );
}
