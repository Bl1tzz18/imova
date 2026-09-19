import { redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { LinkButton } from "@/components/ui/Button";
import { OwnerListingsList } from "@/components/property/OwnerListingsList";
import { getSessionToken } from "@/lib/auth/session";
import type { Property } from "@/types/property";

async function getMyProperties(token: string): Promise<Property[]> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/users/me/properties`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch my listings: ${res.status}`);
  }

  return res.json();
}

export default async function MyListingsPage() {
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/my-listings");
  }

  const [properties, t] = await Promise.all([
    getMyProperties(token),
    getTranslations("MyListingsPage"),
  ]);

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h1 className="font-display text-2xl font-medium text-ink-950 sm:text-3xl">{t("title")}</h1>
              <p className="mt-1 text-sm text-ink-500">{t("subtitle")}</p>
            </div>
            <LinkButton href="/properties/new">{t("addNewListing")}</LinkButton>
          </div>

          <div className="mt-8">
            <OwnerListingsList properties={properties} />
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
}
