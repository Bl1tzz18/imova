import { notFound, redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { LinkButton } from "@/components/ui/Button";
import { EditListingForm } from "@/components/property/EditListingForm";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getSessionToken } from "@/lib/auth/session";
import type { Property } from "@/types/property";

async function getProperty(id: string, token: string): Promise<Property | null> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/properties/${id}`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  if (res.status === 404) {
    return null;
  }

  if (!res.ok) {
    throw new Error(`Failed to fetch property: ${res.status}`);
  }

  return res.json();
}

export default async function EditListingPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const token = await getSessionToken();
  if (!token) {
    redirect(`/login?next=${encodeURIComponent(`/my-listings/${id}/edit`)}`);
  }

  const [property, profile, t] = await Promise.all([
    getProperty(id, token),
    getCurrentUserProfile(),
    getTranslations("EditListingPage"),
  ]);

  if (!property) {
    notFound();
  }

  const isOwner = profile != null && (profile.id === property.ownerId || profile.roles.includes("Admin"));

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <div className="mx-auto max-w-2xl px-4 py-10 sm:px-6 sm:py-14">
          <LinkButton href="/my-listings" variant="ghost" size="sm" className="!px-0 !justify-start">
            ← {t("backToMyListings")}
          </LinkButton>

          <h1 className="mt-4 font-display text-2xl font-medium text-ink-950 sm:text-3xl">{t("title")}</h1>

          {isOwner ? (
            <>
              <p className="mt-2 text-sm text-ink-500">{t("subtitle")}</p>
              <div className="mt-8">
                <EditListingForm property={property} />
              </div>
            </>
          ) : (
            <p className="mt-4 rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
              {t("forbiddenText")}
            </p>
          )}
        </div>
      </main>
      <Footer />
    </div>
  );
}
