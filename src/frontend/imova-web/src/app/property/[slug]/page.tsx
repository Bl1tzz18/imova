import Link from "next/link";
import { notFound } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { Badge } from "@/components/ui/Badge";
import { LinkButton } from "@/components/ui/Button";
import { PropertyGallery } from "@/components/property/PropertyGallery";
import { formatDate, formatFullLocation, formatPrice } from "@/lib/utils/format";
import type { Property } from "@/types/property";

async function getProperty(id: string): Promise<Property | null> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/properties/${id}`, { cache: "no-store" });

  if (res.status === 404) {
    return null;
  }

  if (!res.ok) {
    throw new Error(`Failed to fetch property: ${res.status}`);
  }

  return res.json();
}

export default async function ProprietatePage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug: id } = await params;
  const property = await getProperty(id);

  if (!property) {
    notFound();
  }

  const [locale, t, tType, tListing, tCard] = await Promise.all([
    getLocale(),
    getTranslations("PropertyDetail"),
    getTranslations("PropertyType"),
    getTranslations("ListingType"),
    getTranslations("PropertyCard"),
  ]);

  const location = formatFullLocation(property.location);

  const facts: { label: string; value: string }[] = [];
  if (property.area != null) facts.push({ label: t("area"), value: `${property.area} m²` });
  if (property.rooms != null) facts.push({ label: t("rooms"), value: String(property.rooms) });
  if (property.bathrooms != null) facts.push({ label: t("bathrooms"), value: String(property.bathrooms) });
  if (property.floor != null) {
    facts.push({
      label: t("floor"),
      value:
        property.totalFloors != null
          ? t("floorOf", { floor: property.floor, totalFloors: property.totalFloors })
          : String(property.floor),
    });
  }
  if (property.yearBuilt != null) facts.push({ label: t("yearBuilt"), value: String(property.yearBuilt) });
  if (property.furnished != null) facts.push({ label: t("furnished"), value: property.furnished ? t("yes") : t("no") });
  if (property.parkingAvailable != null)
    facts.push({ label: t("parkingAvailable"), value: property.parkingAvailable ? t("yes") : t("no") });
  if (property.petsAllowed != null)
    facts.push({ label: t("petsAllowed"), value: property.petsAllowed ? t("yes") : t("no") });

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <div className="mx-auto max-w-5xl px-4 py-8 sm:px-6">
          <Link
            href="/"
            className="inline-flex items-center gap-1.5 text-sm font-medium text-ink-500 transition-colors hover:text-ink-900"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="h-4 w-4">
              <path d="m15 18-6-6 6-6" />
            </svg>
            {t("back")}
          </Link>

          <PropertyGallery media={property.media} title={property.title} propertyType={property.propertyType} />

          <div className="mt-6 flex flex-col gap-8 lg:flex-row lg:items-start">
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <Badge tone={property.listingType === "Rent" ? "accent" : "brand"}>
                  {tListing(property.listingType)}
                </Badge>
                <Badge tone="neutral">{tType(property.propertyType)}</Badge>
              </div>

              <h1 className="mt-3 text-balance font-display text-3xl font-medium text-ink-950 sm:text-4xl">
                {property.title}
              </h1>

              {location && (
                <p className="mt-2 flex flex-wrap items-center gap-x-1.5 gap-y-1 text-sm text-ink-500">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" className="h-4 w-4 shrink-0">
                    <path d="M12 21s-7-6.1-7-11a7 7 0 1 1 14 0c0 4.9-7 11-7 11Z" />
                    <circle cx="12" cy="10" r="2.5" />
                  </svg>
                  {location}
                  {property.location && (
                    <a
                      href={`https://www.google.com/maps?q=${property.location.latitude},${property.location.longitude}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="font-medium text-brand-700 underline-offset-2 hover:underline"
                    >
                      {t("viewOnMap")}
                    </a>
                  )}
                </p>
              )}

              <div className="mt-8">
                <h2 className="font-display text-xl font-medium text-ink-950">{t("description")}</h2>
                <p className="mt-2 whitespace-pre-line text-sm leading-relaxed text-ink-600">
                  {property.description}
                </p>
              </div>

              {facts.length > 0 && (
                <div className="mt-8">
                  <h2 className="font-display text-xl font-medium text-ink-950">{t("details")}</h2>
                  <dl className="mt-3 grid grid-cols-2 gap-3 sm:grid-cols-3">
                    {facts.map((fact) => (
                      <div key={fact.label} className="rounded-xl border border-ink-100 bg-white px-4 py-3">
                        <dt className="text-[11px] uppercase tracking-wide text-ink-400">{fact.label}</dt>
                        <dd className="mt-1 text-sm font-semibold text-ink-900">{fact.value}</dd>
                      </div>
                    ))}
                  </dl>
                </div>
              )}
            </div>

            <aside className="w-full shrink-0 lg:w-80">
              <div className="rounded-2xl border border-ink-100 bg-white p-6 shadow-[var(--shadow-card)] lg:sticky lg:top-24">
                <p className="font-display text-3xl font-semibold text-ink-950">
                  {formatPrice(property.price, property.currency)}
                  {property.listingType === "Rent" && (
                    <span className="ml-1 text-base font-normal text-ink-500">{tCard("perMonth")}</span>
                  )}
                </p>

                <div className="mt-4 space-y-1 border-t border-ink-100 pt-4 text-xs text-ink-400">
                  <p>{t("listedOn", { date: formatDate(property.publishedAt ?? property.createdAt, locale) })}</p>
                  {property.updatedAt !== property.createdAt && (
                    <p>{t("updatedOn", { date: formatDate(property.updatedAt, locale) })}</p>
                  )}
                </div>
              </div>

              {property.owner && (
                <div className="mt-4 rounded-2xl border border-ink-100 bg-white p-6 shadow-[var(--shadow-card)]">
                  <h2 className="font-display text-base font-medium text-ink-950">{t("contactOwner")}</h2>
                  <dl className="mt-3 space-y-3 text-sm">
                    <div>
                      <dt className="text-[11px] uppercase tracking-wide text-ink-400">{t("email")}</dt>
                      <dd className="mt-0.5">
                        <a href={`mailto:${property.owner.email}`} className="font-medium text-brand-700 hover:underline">
                          {property.owner.email}
                        </a>
                      </dd>
                    </div>
                    <div>
                      <dt className="text-[11px] uppercase tracking-wide text-ink-400">{t("phone")}</dt>
                      <dd className="mt-0.5">
                        {property.owner.phone ? (
                          <a href={`tel:${property.owner.phone}`} className="font-medium text-brand-700 hover:underline">
                            {property.owner.phone}
                          </a>
                        ) : (
                          <span className="text-ink-400">{t("phoneNotProvided")}</span>
                        )}
                      </dd>
                    </div>
                  </dl>
                </div>
              )}
            </aside>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug: id } = await params;
  const property = await getProperty(id);

  return { title: property ? `${property.title} — IMOVA` : "IMOVA" };
}
