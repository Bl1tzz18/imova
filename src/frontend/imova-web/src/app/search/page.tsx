import { getTranslations } from "next-intl/server";
import { PropertyCard } from "@/components/property/PropertyCard";
import { getSessionToken } from "@/lib/auth/session";
import type { Property } from "@/types/property";

const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;
type PropertyTypeFilter = (typeof PROPERTY_TYPES)[number];

function isPropertyType(value: string): value is PropertyTypeFilter {
  return (PROPERTY_TYPES as readonly string[]).includes(value);
}

async function getProperties(propertyType?: PropertyTypeFilter): Promise<Property[]> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  const query = propertyType ? `?propertyType=${propertyType}` : "";
  const res = await fetch(`${apiUrl}/api/v1/properties${query}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch properties: ${res.status}`);
  }

  return res.json();
}

export default async function CautaPage({
  searchParams,
}: {
  searchParams: Promise<{ propertyType?: string }>;
}) {
  const { propertyType } = await searchParams;
  const typeFilter = propertyType && isPropertyType(propertyType) ? propertyType : undefined;

  const [properties, t, tType] = await Promise.all([
    getProperties(typeFilter),
    getTranslations("SearchPage"),
    getTranslations("PropertyType"),
  ]);

  const title = typeFilter ? tType(typeFilter) : t("title");

  return (
    <main className="mx-auto max-w-6xl px-4 py-10 sm:px-6">
      <h1 className="font-display text-2xl font-medium text-ink-950 sm:text-3xl">{title}</h1>
      <p className="mt-1 text-sm text-ink-500">
        {properties.length > 0
          ? t("resultsCount", { count: properties.length })
          : t("noResults")}
      </p>

      {properties.length > 0 ? (
        <div className="mt-8 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {properties.map((property) => (
            <PropertyCard key={property.id} property={property} />
          ))}
        </div>
      ) : (
        <div className="mt-8 flex flex-col items-center gap-4 rounded-2xl border border-dashed border-ink-200 bg-white px-6 py-16 text-center">
          <p className="text-sm text-ink-500">{t("noResults")}</p>
        </div>
      )}
    </main>
  );
}
