import Link from "next/link";
import { notFound } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { Badge } from "@/components/ui/Badge";
import { LinkButton } from "@/components/ui/Button";
import { PropertyGallery } from "@/components/property/PropertyGallery";
import { PropertyLocationPreview } from "@/components/property/PropertyLocationPreview";
import { SaveListingButton } from "@/components/property/SaveListingButton";
import { formatDate, formatFullLocation, formatPrice } from "@/lib/utils/format";
import { squareMetersToAri } from "@/lib/listing/view";
import { attributeSchemaFor } from "@/lib/property/attributeSchema";
import { getSessionToken } from "@/lib/auth/session";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getConversationIdForListing } from "@/lib/messaging/api";
import { messageButtonEmphasis, messageButtonHref, showsRelayNotice } from "@/lib/messaging/contact";
import type { Listing } from "@/types/listing";

async function getListing(id: string): Promise<Listing | null> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  const res = await fetch(`${apiUrl}/api/v1/listings/${id}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    cache: "no-store",
  });

  if (res.status === 404) {
    return null;
  }

  if (!res.ok) {
    throw new Error(`Failed to fetch listing: ${res.status}`);
  }

  return res.json();
}

export default async function ProprietatePage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug: id } = await params;
  const listing = await getListing(id);

  if (!listing) {
    notFound();
  }

  const [locale, t, tType, tListing, tCard, tAttr, tCondition, tAmenity, tProximity, tMethod] = await Promise.all([
    getLocale(),
    getTranslations("PropertyDetail"),
    getTranslations("PropertyType"),
    getTranslations("ListingType"),
    getTranslations("PropertyCard"),
    getTranslations("Attributes"),
    getTranslations("Condition"),
    getTranslations("Amenity"),
    getTranslations("Proximity"),
    getTranslations("ContactMethod"),
  ]);

  const location = formatFullLocation(listing.property.location);

  const { property, rentalDetails, publisher, contact } = listing;

  // "Scrie mesaj" always reaches the publisher's own inbox (never the listing's "Other" contact).
  const profile = await getCurrentUserProfile();
  const isOwner = profile?.id === publisher.userId;
  const existingConversationId = profile && !isOwner ? await getConversationIdForListing(listing.id) : null;
  const messageHref = messageButtonHref(listing.id, profile !== null, existingConversationId);
  const messageEmphasis = messageButtonEmphasis(contact);
  const messageButton = (
    <LinkButton href={messageHref} variant={messageEmphasis} className="mt-4 w-full">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4" aria-hidden>
        <path d="M4 5h16v11H8l-4 4V5Z" strokeLinejoin="round" />
      </svg>
      {existingConversationId ? t("openConversation") : t("sendMessage")}
    </LinkButton>
  );
  const yesNo = (value: boolean) => (value ? t("yes") : t("no"));

  // Physical facts: area, building data, then whatever the property type's attribute schema
  // defines (see ATTRIBUTE_SCHEMA) — only the fields actually filled in are shown.
  const facts: { label: string; value: string }[] = [];
  facts.push({
    label: t("area"),
    value:
      property.propertyType === "Land"
        ? `${property.totalAreaM2} m² (${squareMetersToAri(property.totalAreaM2)} ari)`
        : `${property.totalAreaM2} m²`,
  });
  if (property.yearBuilt != null) facts.push({ label: t("yearBuilt"), value: String(property.yearBuilt) });
  if (property.condition) facts.push({ label: t("condition"), value: tCondition(property.condition) });

  const attributes = property.typeSpecificAttributes;
  // Below-ground levels read better by name: -1 = basement (subsol), 0 = semi-basement (demisol).
  const formatFloor = (floor: unknown) => {
    const n = Number(floor);
    if (n < 0) return `${tAttr("floorLevels.basement")} (${n})`;
    if (n === 0) return `${tAttr("floorLevels.semiBasement")} (0)`;
    return String(floor);
  };
  for (const field of attributeSchemaFor(property.propertyType)) {
    const value = attributes[field.name];
    // totalFloors is folded into the floor fact ("3 of 9") when both are present.
    if (value == null || (field.name === "totalFloors" && typeof attributes.floor === "number")) continue;
    // Form labels carry their unit ("Living area (m²)"); here the value already shows it.
    const label = (t.has(field.name) ? t(field.name) : tAttr(`${field.name}.label`)).replace(/\s*\((m²|m|м²|м)\)$/, "");

    if (field.name === "floor" && typeof attributes.totalFloors === "number") {
      facts.push({ label, value: t("floorOf", { floor: formatFloor(value), totalFloors: attributes.totalFloors }) });
    } else if (field.name === "floor") {
      facts.push({ label, value: formatFloor(value) });
    } else if (field.kind === "enum") {
      facts.push({ label, value: tAttr(`${field.name}.options.${String(value)}`) });
    } else if (field.kind === "yesno") {
      facts.push({ label, value: yesNo(value === true) });
    } else if (field.name.endsWith("AreaM2")) {
      facts.push({ label, value: `${String(value)} m²` });
    } else if (field.name === "ceilingHeightM") {
      facts.push({ label, value: `${String(value)} m` });
    } else {
      facts.push({ label, value: String(value) });
    }
  }

  // Terms of this particular rental offer, not of the property itself.
  const rentalFacts: { label: string; value: string }[] = [];
  if (rentalDetails) {
    if (rentalDetails.minLeasePeriodMonths != null) {
      rentalFacts.push({ label: t("minLeasePeriod"), value: t("months", { count: rentalDetails.minLeasePeriodMonths }) });
    }
    if (rentalDetails.securityDepositAmount != null) {
      rentalFacts.push({
        label: t("securityDeposit"),
        value: formatPrice(rentalDetails.securityDepositAmount, listing.price.currency),
      });
    }
    if (rentalDetails.availableFrom) {
      rentalFacts.push({ label: t("availableFrom"), value: formatDate(rentalDetails.availableFrom, locale) });
    }
    rentalFacts.push({ label: t("utilitiesIncluded"), value: yesNo(rentalDetails.utilitiesIncluded) });
    if (rentalDetails.petsAllowed != null) {
      rentalFacts.push({ label: t("petsAllowed"), value: yesNo(rentalDetails.petsAllowed) });
    }
  }

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

          <PropertyGallery media={listing.photos} title={listing.title} propertyType={property.propertyType} />

          <div className="mt-6 flex flex-col gap-8 lg:flex-row lg:items-start">
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <div className="flex flex-wrap items-center gap-2">
                  <Badge tone={listing.transactionType === "Rent" ? "accent" : "brand"}>
                    {tListing(listing.transactionType)}
                  </Badge>
                  <Badge tone="neutral">{tType(listing.property.propertyType)}</Badge>
                </div>
                <SaveListingButton listingId={listing.id} initialSaved={listing.isSaved} variant="labeled" />
              </div>

              <h1 className="mt-3 text-balance font-display text-3xl font-medium text-ink-950 sm:text-4xl">
                {listing.title}
              </h1>

              {location && (
                <p className="mt-2 flex flex-wrap items-center gap-x-1.5 gap-y-1 text-sm text-ink-500">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" className="h-4 w-4 shrink-0">
                    <path d="M12 21s-7-6.1-7-11a7 7 0 1 1 14 0c0 4.9-7 11-7 11Z" />
                    <circle cx="12" cy="10" r="2.5" />
                  </svg>
                  {location}
                </p>
              )}

              {listing.property.location &&
                (listing.property.location.latitude != null && listing.property.location.longitude != null ? (
                  <div className="mt-4">
                    <PropertyLocationPreview
                      listing={listing}
                      lat={listing.property.location.latitude}
                      lng={listing.property.location.longitude}
                    />
                  </div>
                ) : (
                  <p className="mt-4 rounded-xl border border-dashed border-ink-200 bg-ink-50 px-3.5 py-2.5 text-xs text-ink-500">
                    {t("locationPending")}
                  </p>
                ))}

              <div className="mt-8">
                <h2 className="font-display text-xl font-medium text-ink-950">{t("description")}</h2>
                <p className="mt-2 whitespace-pre-line text-sm leading-relaxed text-ink-600">
                  {listing.description}
                </p>
              </div>

              {facts.length > 0 && (
                <div className="mt-8">
                  <h2 className="font-display text-xl font-medium text-ink-950">{t("details")}</h2>
                  <FactGrid facts={facts} />
                </div>
              )}

              {property.amenities.length > 0 && (
                <div className="mt-8">
                  <h2 className="font-display text-xl font-medium text-ink-950">{t("amenities")}</h2>
                  <ul className="mt-3 flex flex-wrap gap-2">
                    {property.amenities.map((amenity) => (
                      <li
                        key={amenity.id}
                        className="rounded-full border border-ink-100 bg-white px-3 py-1.5 text-sm text-ink-700"
                      >
                        {tAmenity.has(amenity.key) ? tAmenity(amenity.key) : amenity.labelRo}
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              {property.proximities.length > 0 && (
                <div className="mt-8">
                  <h2 className="font-display text-xl font-medium text-ink-950">{t("proximities")}</h2>
                  <ul className="mt-3 flex flex-wrap gap-2">
                    {property.proximities.map((proximity) => (
                      <li
                        key={proximity.id}
                        className="rounded-full border border-ink-100 bg-white px-3 py-1.5 text-sm text-ink-700"
                      >
                        {tProximity.has(proximity.key) ? tProximity(proximity.key) : proximity.labelRo}
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              {rentalFacts.length > 0 && (
                <div className="mt-8">
                  <h2 className="font-display text-xl font-medium text-ink-950">{t("rentalTerms")}</h2>
                  <FactGrid facts={rentalFacts} />
                </div>
              )}
            </div>

            <aside className="w-full shrink-0 lg:w-80">
              <div className="rounded-2xl border border-ink-100 bg-white p-6 shadow-[var(--shadow-card)] lg:sticky lg:top-24">
                <p className="font-display text-3xl font-semibold text-ink-950">
                  {formatPrice(listing.price.amount, listing.price.currency)}
                  {listing.transactionType === "Rent" && (
                    <span className="ml-1 text-base font-normal text-ink-500">{tCard("perMonth")}</span>
                  )}
                </p>
                {listing.price.currency !== "EUR" && (
                  <p className="mt-1 text-sm text-ink-500">
                    {t("approxEur", { price: formatPrice(listing.price.priceEur, "EUR") })}
                  </p>
                )}
                {listing.price.isNegotiable && (
                  <Badge tone="neutral" className="mt-3">
                    {t("negotiable")}
                  </Badge>
                )}

                <div className="mt-4 space-y-1 border-t border-ink-100 pt-4 text-xs text-ink-400">
                  <p>{t("listedOn", { date: formatDate(listing.publishedAt ?? listing.createdAt, locale) })}</p>
                  {listing.updatedAt !== listing.createdAt && (
                    <p>{t("updatedOn", { date: formatDate(listing.updatedAt, locale) })}</p>
                  )}
                </div>
              </div>

              {contact && (
                <div className="mt-4 rounded-2xl border border-ink-100 bg-white p-6 shadow-[var(--shadow-card)]">
                  <h2 className="font-display text-base font-medium text-ink-950">{t("contactOwner")}</h2>
                  <p className="mt-3 flex flex-wrap items-center gap-2 text-sm font-semibold text-ink-900">
                    {contact.name ?? publisher.displayName}
                    {contact.personType === "Self" && publisher.publisherType === "Agency" && (
                      <Badge tone="brand">{t("agency")}</Badge>
                    )}
                  </p>
                  {contact.personType === "Self" && publisher.bio && (
                    <p className="mt-1 text-xs text-ink-500">{publisher.bio}</p>
                  )}
                  {/* With the phone hidden, messaging is the main way to reach them — shown first. */}
                  {!isOwner && messageEmphasis === "primary" && messageButton}
                  <dl className="mt-3 space-y-3 text-sm">
                    {contact.email && (
                      <div>
                        <dt className="text-[11px] uppercase tracking-wide text-ink-400">{t("email")}</dt>
                        <dd className="mt-0.5">
                          <a href={`mailto:${contact.email}`} className="font-medium text-brand-700 hover:underline">
                            {contact.email}
                          </a>
                        </dd>
                      </div>
                    )}
                    <div>
                      <dt className="text-[11px] uppercase tracking-wide text-ink-400">{t("phone")}</dt>
                      <dd className="mt-0.5">
                        {contact.phone ? (
                          <a href={`tel:${contact.phone}`} className="font-medium text-brand-700 hover:underline">
                            {contact.phone}
                          </a>
                        ) : (
                          <span className="text-ink-400">
                            {contact.hidePhoneNumber ? t("phoneHidden") : t("phoneNotProvided")}
                          </span>
                        )}
                      </dd>
                    </div>
                    {contact.phone && contact.messagingApps.length > 0 && (
                      <div>
                        <dt className="text-[11px] uppercase tracking-wide text-ink-400">{t("availableOn")}</dt>
                        <dd className="mt-1 flex flex-wrap gap-1.5">
                          {contact.messagingApps.map((app) => (
                            <Badge key={app} tone="neutral">
                              {app}
                            </Badge>
                          ))}
                        </dd>
                      </div>
                    )}
                    {contact.preferredContactMethod !== "Any" && (
                      <div>
                        <dt className="text-[11px] uppercase tracking-wide text-ink-400">{t("preferredContact")}</dt>
                        <dd className="mt-0.5 text-ink-900">{tMethod(contact.preferredContactMethod)}</dd>
                      </div>
                    )}
                    {contact.phone && contact.callHoursFrom && contact.callHoursTo && (
                      <div>
                        <dt className="text-[11px] uppercase tracking-wide text-ink-400">{t("callHours")}</dt>
                        <dd className="mt-0.5 text-ink-900">
                          {contact.callHoursFrom}–{contact.callHoursTo}
                        </dd>
                      </div>
                    )}
                  </dl>
                  {!isOwner && messageEmphasis === "secondary" && messageButton}
                  {showsRelayNotice(contact, isOwner) && (
                    <p className="mt-4 rounded-xl border border-accent-100 bg-accent-100/40 px-3.5 py-3 text-xs text-ink-700">
                      {t("relayNotice", { name: contact.name ?? "" })}
                    </p>
                  )}
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
  const listing = await getListing(id);

  return { title: listing ? `${listing.title} — IMOVA` : "IMOVA" };
}

function FactGrid({ facts }: { facts: { label: string; value: string }[] }) {
  return (
    <dl className="mt-3 grid grid-cols-2 gap-3 sm:grid-cols-3">
      {facts.map((fact) => (
        <div key={fact.label} className="rounded-xl border border-ink-100 bg-white px-4 py-3">
          <dt className="text-[11px] uppercase tracking-wide text-ink-400">{fact.label}</dt>
          <dd className="mt-1 text-sm font-semibold text-ink-900">{fact.value}</dd>
        </div>
      ))}
    </dl>
  );
}
