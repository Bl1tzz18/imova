import { notFound } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { Badge } from "@/components/ui/Badge";
import { LinkButton } from "@/components/ui/Button";
import { PropertyGallery } from "@/components/property/PropertyGallery";
import { ListingDetails, ListingKeyFacts, ListingRentalTerms } from "@/components/property/ListingDetails";
import { PropertyLocationPreview } from "@/components/property/PropertyLocationPreview";
import { SaveListingButton } from "@/components/property/SaveListingButton";
import { ReportListingButton } from "@/components/property/ReportListingButton";
import { ContactSheet } from "@/components/property/ContactSheet";
import { ContactActions } from "@/components/property/ContactActions";
import { Avatar } from "@/components/ui/Avatar";
import { BackLink } from "@/components/layout/BackLink";
import { formatDate, formatFullLocation, formatPrice } from "@/lib/utils/format";
import { getSessionToken } from "@/lib/auth/session";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getConversationIdForListing } from "@/lib/messaging/api";
import { messageButtonEmphasis, messageButtonHref, mobileContactBar, showsRelayNotice } from "@/lib/messaging/contact";
import { contactRole, isMessagingApp, maskPhone } from "@/lib/listing/contactCard";
import { listingBackHref } from "@/lib/navigation/history";
import { cn } from "@/lib/utils/cn";
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

  const [locale, t, tType, tListing, tCard, tMethod] = await Promise.all([
    getLocale(),
    getTranslations("PropertyDetail"),
    getTranslations("PropertyType"),
    getTranslations("ListingType"),
    getTranslations("PropertyCard"),
    getTranslations("ContactMethod"),
  ]);

  const location = formatFullLocation(listing.property.location);

  const { publisher, contact } = listing;

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
  // The price, its lease terms and the dates — beside the photos on a desktop, right under them on
  // a phone (never after all the details, where it used to end up).
  const priceCard = (
    <div className="rounded-2xl border border-ink-100 bg-white p-6 shadow-[var(--shadow-card)]">
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

      <ListingRentalTerms listing={listing} />

      <div className="mt-4 space-y-1 border-t border-ink-100 pt-4 text-xs text-ink-400">
        <p>{t("listedOn", { date: formatDate(listing.publishedAt ?? listing.createdAt, locale) })}</p>
        {listing.updatedAt !== listing.createdAt && (
          <p>{t("updatedOn", { date: formatDate(listing.updatedAt, locale) })}</p>
        )}
      </div>
    </div>
  );

  // Who to contact and how — at the end of the page, and in the phone bar's "Date de contact" sheet.
  // The person first (photo, name, and what they are: an agency's agent, a private person, …), then
  // the ways to reach them: the number — half-hidden until asked for, with the hours they take
  // calls right under it — the messaging apps they use, and messages on IMOVA. No email: the API
  // only gives it to the owner and admins.
  const role = contact && contactRole(contact);
  const apps = contact?.messagingApps.filter(isMessagingApp) ?? [];
  const contactCard = contact && role && (
    <div className="rounded-2xl border border-ink-100 bg-white p-6 shadow-[var(--shadow-card)] lg:mt-4">
      <h2 className="font-display text-lg font-semibold text-ink-950">{t("contactOwner")}</h2>

      <div className="mt-4 flex items-center gap-3.5">
        <Avatar
          userId={publisher.userId}
          displayName={contact.name ?? publisher.displayName}
          pictureUrl={contact.pictureUrl}
          size={52}
          className="shrink-0 ring-2 ring-white shadow-sm"
        />
        <div className="min-w-0">
          <p className="truncate text-base font-semibold text-ink-950">{contact.name ?? publisher.displayName}</p>
          <p className="text-sm text-ink-500">
            {t(
              role.kind === "agent"
                ? "roleAgent"
                : role.kind === "agency"
                  ? "roleAgency"
                  : role.kind === "individual"
                    ? "roleIndividual"
                    : "roleContactPerson",
            )}
          </p>
          {/* The agency on its own line, so a long name wraps instead of being cut off. */}
          {role.kind === "agent" && <p className="text-sm font-medium leading-5 text-ink-800">{role.agencyName}</p>}
        </div>
      </div>
      {contact.personType === "Self" && publisher.bio && (
        <p className="mt-3 line-clamp-3 text-xs leading-5 text-ink-500">{publisher.bio}</p>
      )}

      {/* With the phone hidden, messaging is the main way to reach them — shown first. */}
      {!isOwner && messageEmphasis === "primary" && messageButton}

      {contact.phone ? (
        <ContactActions
          listingId={listing.id}
          maskedPhone={maskPhone(contact.phone)}
          ownPhone={isOwner ? contact.phone : null}
          apps={apps}
          callHours={
            contact.callHoursFrom && contact.callHoursTo
              ? t("callHoursBetween", { from: contact.callHoursFrom, to: contact.callHoursTo })
              : null
          }
        />
      ) : (
        <p className="mt-4 rounded-xl bg-ink-50 px-3.5 py-3 text-sm text-ink-600">
          {contact.hidePhoneNumber ? t("phoneHidden") : t("phoneNotProvided")}
        </p>
      )}

      {!isOwner && messageEmphasis === "secondary" && messageButton}

      {contact.preferredContactMethod !== "Any" && (
        <p className="mt-5 flex items-center gap-2.5 border-t border-ink-100 pt-4 text-sm text-ink-600">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" className="h-4 w-4 shrink-0 text-ink-400" aria-hidden>
            <path d="m5 12 5 5L20 7" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
          <span>
            {t("preferredContact")}: <span className="font-medium text-ink-800">{tMethod(contact.preferredContactMethod)}</span>
          </span>
        </p>
      )}

      {showsRelayNotice(contact, isOwner) && (
        <p className="mt-4 rounded-xl border border-accent-100 bg-accent-100/40 px-3.5 py-3 text-xs text-ink-700">
          {t("relayNotice", { name: contact.name ?? "" })}
        </p>
      )}
    </div>
  );

  const contactBar = mobileContactBar(contact, isOwner);

  return (
    <div className={cn("flex min-h-screen flex-col", contactBar && "pb-20 lg:pb-0")}>
      <main className="flex-1">
        <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
          {/* Back to the visitor's last search (filters, sort and page as they left them); from outside
              the site, to a search for the same kind of listing. */}
          <BackLink
            href={listingBackHref(null, { transactionType: listing.transactionType, propertyType: listing.property.propertyType })}
            label={t("back")}
            toLastSearch
          />

          {/* Title, location and save above; then the photos with the price & contact beside them. */}
          <div className="mt-4">
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
          </div>

          <div className="mt-5 flex flex-col gap-8 lg:flex-row lg:items-start">
            <div className="min-w-0 flex-1">
              <PropertyGallery media={listing.photos} title={listing.title} propertyType={listing.property.propertyType} />

              <div className="mt-5 lg:hidden">{priceCard}</div>

              <ListingKeyFacts listing={listing} />

              {/* The owner's own words — the first thing most people read, so it gets a card of its
                  own and body-size text, kept to a comfortable line length. */}
              <section className="mt-8 rounded-2xl border border-ink-100 bg-white p-6 sm:p-7" aria-labelledby="listing-description-title">
                <h2 id="listing-description-title" className="font-display text-xl font-medium text-ink-950">
                  {t("description")}
                </h2>
                <p className="mt-3 max-w-[70ch] whitespace-pre-line text-[15px] leading-7 text-ink-800 sm:text-base">
                  {listing.description}
                </p>
              </section>

              <ListingDetails listing={listing} />

              {listing.property.location && (
                <section className="mt-10" aria-labelledby="listing-location-title">
                  <h2 id="listing-location-title" className="font-display text-xl font-medium text-ink-950">
                    {t("locationTitle")}
                  </h2>
                  {location && <p className="mt-1 text-sm text-ink-500">{location}</p>}
                  <div className="mt-4">
                    {listing.property.location.latitude != null && listing.property.location.longitude != null ? (
                      // A client component: everything it gets is serialized into the page, so never
                      // the contact (the full phone number is only fetched on "Arată").
                      <PropertyLocationPreview
                        listing={{ ...listing, contact: null }}
                        lat={listing.property.location.latitude}
                        lng={listing.property.location.longitude}
                      />
                    ) : (
                      <p className="rounded-xl border border-dashed border-ink-200 bg-ink-50 px-3.5 py-2.5 text-xs text-ink-500">
                        {t("locationPending")}
                      </p>
                    )}
                  </div>
                </section>
              )}
            </div>

            <aside className="w-full shrink-0 lg:w-80">
              {/* Desktop: the price card beside the photos. (On a phone it's under the photos.) */}
              <div className="hidden lg:block">{priceCard}</div>

              {contactCard}

              {/* Only a live listing can be reported, and never by its owner. */}
              {!isOwner && listing.status === "Active" && (
                <div className="px-1">
                  <ReportListingButton listingId={listing.id} signedIn={profile !== null} />
                </div>
              )}
            </aside>
          </div>
        </div>
      </main>

      <Footer />

      {/* Phones: calling or messaging stays one tap away wherever the visitor has scrolled to. */}
      {contactBar && (
        <div className="fixed inset-x-0 bottom-0 z-30 border-t border-ink-100 bg-white/95 px-4 pb-[max(0.75rem,env(safe-area-inset-bottom))] pt-3 backdrop-blur lg:hidden">
          <div className="mx-auto flex max-w-md gap-2">
            {contactBar.contactDetails && (
              <ContactSheet label={t("contactDetails")} closeLabel={t("closeContactDetails")} primary={contactBar.primary === "contactDetails"}>
                {contactCard}
              </ContactSheet>
            )}
            <LinkButton href={messageHref} variant={contactBar.primary === "message" ? "primary" : "secondary"} className="flex-1">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4" aria-hidden>
                <path d="M4 5h16v11H8l-4 4V5Z" strokeLinejoin="round" />
              </svg>
              {existingConversationId ? t("openConversation") : t("sendMessage")}
            </LinkButton>
          </div>
        </div>
      )}
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
