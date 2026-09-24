import { getTranslations } from "next-intl/server";
import { PropertyCard } from "@/components/property/PropertyCard";
import { getSessionToken } from "@/lib/auth/session";
import type { Listing } from "@/types/listing";

const PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;
type PropertyTypeFilter = (typeof PROPERTY_TYPES)[number];

function isPropertyType(value: string): value is PropertyTypeFilter {
  return (PROPERTY_TYPES as readonly string[]).includes(value);
}

const TRANSACTION_TYPES = ["Sale", "Rent"] as const;
type TransactionTypeFilter = (typeof TRANSACTION_TYPES)[number];

function isTransactionType(value: string): value is TransactionTypeFilter {
  return (TRANSACTION_TYPES as readonly string[]).includes(value);
}

async function getListings(propertyType?: PropertyTypeFilter, transactionType?: TransactionTypeFilter): Promise<Listing[]> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  const params = new URLSearchParams();
  if (propertyType) params.set("propertyType", propertyType);
  if (transactionType) params.set("transactionType", transactionType);
  const query = params.size > 0 ? `?${params}` : "";
  const res = await fetch(`${apiUrl}/api/v1/listings${query}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch listings: ${res.status}`);
  }

  return res.json();
}

export default async function CautaPage({
  searchParams,
}: {
  searchParams: Promise<{ propertyType?: string; transactionType?: string }>;
}) {
  const { propertyType, transactionType } = await searchParams;
  const typeFilter = propertyType && isPropertyType(propertyType) ? propertyType : undefined;
  const transactionFilter = transactionType && isTransactionType(transactionType) ? transactionType : undefined;

  const [listings, t, tType] = await Promise.all([
    getListings(typeFilter, transactionFilter),
    getTranslations("SearchPage"),
    getTranslations("PropertyType"),
  ]);

  const title = typeFilter ? tType(typeFilter) : t("title");

  return (
    <main className="mx-auto max-w-6xl px-4 py-10 sm:px-6">
      <h1 className="font-display text-2xl font-medium text-ink-950 sm:text-3xl">{title}</h1>
      <p className="mt-1 text-sm text-ink-500">
        {listings.length > 0
          ? t("resultsCount", { count: listings.length })
          : t("noResults")}
      </p>

      {listings.length > 0 ? (
        <div className="mt-8 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {listings.map((listing) => (
            <PropertyCard key={listing.id} listing={listing} />
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
