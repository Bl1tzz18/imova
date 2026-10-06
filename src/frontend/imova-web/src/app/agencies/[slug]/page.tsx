import type { Metadata } from "next";
import Link from "next/link";
import { notFound, permanentRedirect } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { BackLink } from "@/components/layout/BackLink";
import { AutoSubmitForm } from "@/components/agency/AutoSubmitForm";
import { ExpandableText } from "@/components/agency/ExpandableText";
import { LinkPagination } from "@/components/agency/LinkPagination";
import { VerifiedBadge } from "@/components/agency/VerifiedBadge";
import { ContactActions } from "@/components/property/ContactActions";
import { PropertyCard } from "@/components/property/PropertyCard";
import { Avatar } from "@/components/ui/Avatar";
import { Button, LinkButton } from "@/components/ui/Button";
import { SelectInput } from "@/components/ui/Field";
import { getAgencyBySlug } from "@/lib/api/agencies";
import { getSessionToken } from "@/lib/auth/session";
import {
  AGENCY_LISTINGS_PAGE_SIZE,
  AGENCY_LISTING_SORTS,
  agencyJsonLd,
  agencyListingsHref,
  agencyListingsSearchQuery,
  agencyMetaDescription,
  agencyPath,
  bioIsLong,
  hasListingFilters,
  parseAgencyListingsParams,
  websiteLabel,
  type AgencyListingsState,
} from "@/lib/agency/publicPage";
import { jsonLdScript } from "@/lib/listing/seo";
import { PROPERTY_TYPES } from "@/lib/property/attributeSchema";
import { siteUrl } from "@/lib/site";
import { cn } from "@/lib/utils/cn";
import { formatMonthYear } from "@/lib/utils/format";
import type { Listing } from "@/types/listing";

type PageProps = {
  params: Promise<{ slug: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

type ListingsPage = { items: Listing[]; totalCount: number };

// The agency's Active listings, through the search (agencyId filter). Signed-in visitors get
// "saved" hearts. Null when the API fails — the page still shows the agency.
async function getAgencyListings(agencyId: string, state: AgencyListingsState): Promise<ListingsPage | null> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  try {
    const res = await fetch(`${apiUrl}/api/v1/listings/search?${agencyListingsSearchQuery(agencyId, state)}`, {
      headers: token ? { Authorization: `Bearer ${token}` } : undefined,
      cache: "no-store",
    });
    return res.ok ? ((await res.json()) as ListingsPage) : null;
  } catch {
    return null;
  }
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const result = await getAgencyBySlug(slug);
  if (result?.kind !== "agency") return {};

  const { agency } = result;
  const t = await getTranslations("Agencies");
  const url = `${siteUrl()}${agencyPath(agency.slug)}`;
  const title = agency.raionName
    ? t("pageTitle", { name: agency.name, city: agency.raionName })
    : t("pageTitleNoCity", { name: agency.name });
  const description = agencyMetaDescription(agency.bio, t("metaFallback", { name: agency.name }));

  return {
    // The whole title is ours ("… | IMOVA"), not the layout's template.
    title: { absolute: title },
    description,
    alternates: { canonical: url },
    openGraph: {
      type: "profile",
      title,
      description,
      url,
      siteName: "IMOVA",
      ...(agency.logoUrl ? { images: [{ url: agency.logoUrl, width: 512, height: 512, alt: t("logoAlt", { name: agency.name }) }] } : {}),
    },
    twitter: { card: "summary", title, description, ...(agency.logoUrl ? { images: [agency.logoUrl] } : {}) },
    // Members still see a deactivated agency; nobody should find it through a search engine.
    robots: agency.status === "Active" ? undefined : { index: false, follow: false },
  };
}

// The agency's public page: who they are and how to reach them, then their active listings with a
// few filters. A slug the agency used before a rename redirects (308) to the current one; an
// unknown or deactivated agency is a 404 (except for its members and admins).
export default async function AgencyPage({ params, searchParams }: PageProps) {
  const { slug } = await params;
  const state = parseAgencyListingsParams(await searchParams);
  const result = await getAgencyBySlug(slug);

  if (!result) {
    notFound();
  }
  if (result.kind === "moved") {
    permanentRedirect(agencyListingsHref(result.slug, state));
  }

  const { agency } = result;
  const [t, tSearch, tType, tListing, locale, listings] = await Promise.all([
    getTranslations("Agencies"),
    getTranslations("Search"),
    getTranslations("PropertyType"),
    getTranslations("ListingType"),
    getLocale(),
    getAgencyListings(agency.id, state),
  ]);

  const total = listings?.totalCount ?? 0;
  const totalPages = Math.ceil(total / AGENCY_LISTINGS_PAGE_SIZE);
  const filtered = hasListingFilters(state);
  const url = `${siteUrl()}${agencyPath(agency.slug)}`;
  const hasPhone = Boolean(agency.phone || agency.phonePrefix);
  const transactionTabs: { value: AgencyListingsState["transactionType"]; label: string }[] = [
    { value: undefined, label: t("filterAll") },
    { value: "Sale", label: tListing("Sale") },
    { value: "Rent", label: tListing("Rent") },
  ];

  return (
    <div className="flex min-h-screen flex-col">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: jsonLdScript(agencyJsonLd(agency, url)) }} />
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 pb-12 sm:px-6">
        <div className="pt-6">
          <BackLink href="/agencies" label={t("backToDirectory")} />
        </div>

        {agency.status !== "Active" && (
          <p role="status" className="mt-4 rounded-xl bg-amber-50 px-4 py-3 text-sm text-amber-900">
            {t("inactiveNotice")}
          </p>
        )}

        {/* Who they are, then how to reach them — side by side on a desktop, one after the other on a phone. */}
        <section className="mt-4 grid gap-6 rounded-2xl border border-ink-100 bg-white p-5 shadow-[var(--shadow-card)] sm:p-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
          <div className="min-w-0">
            <div className="flex items-start gap-4">
              {agency.logoUrl ? (
                // eslint-disable-next-line @next/next/no-img-element -- blob storage URL
                <img
                  src={agency.logoUrl}
                  alt={t("logoAlt", { name: agency.name })}
                  width={96}
                  height={96}
                  className="h-20 w-20 shrink-0 rounded-2xl border border-ink-100 object-cover sm:h-24 sm:w-24"
                />
              ) : (
                <Avatar userId={agency.id} displayName={agency.name} size={80} shape="square" className="shrink-0" />
              )}
              <div className="min-w-0">
                <h1 className="font-hero text-2xl font-extrabold leading-tight text-ink-950 [overflow-wrap:anywhere] sm:text-3xl">
                  {agency.name}
                </h1>
                {agency.isVerified && <VerifiedBadge label className="mt-2" />}
                <p className="mt-2 text-sm text-ink-600">
                  {[agency.raionName, t("onImovaSince", { date: formatMonthYear(agency.createdAt, locale) })]
                    .filter(Boolean)
                    .join(" · ")}
                </p>
                <p className="mt-0.5 text-sm font-medium text-ink-800">{t("activeListings", { count: agency.activeListingCount })}</p>
              </div>
            </div>

            {agency.bio && (
              <ExpandableText
                text={agency.bio}
                long={bioIsLong(agency.bio)}
                moreLabel={t("readMore")}
                lessLabel={t("readLess")}
                className="mt-5"
              />
            )}
            {agency.address && <p className="mt-3 text-sm text-ink-600">{agency.address}</p>}
          </div>

          <div className="lg:border-l lg:border-ink-100 lg:pl-6">
            <h2 className="font-display text-lg font-semibold text-ink-950">{t("contactHeading")}</h2>
            {hasPhone && (
              <ContactActions
                phoneOf={{ agencyId: agency.id }}
                phonePrefix={agency.phonePrefix ?? ""}
                hiddenDigits={agency.phoneHiddenDigits ?? 0}
                ownPhone={agency.phone}
                apps={[]}
                callHours={null}
              />
            )}
            <div className="mt-2.5 grid gap-2.5">
              <LinkButton href={`mailto:${agency.email}`} variant="secondary" className="w-full">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4 shrink-0" aria-hidden>
                  <rect x="3" y="5" width="18" height="14" rx="2" />
                  <path d="m4 7 8 6 8-6" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
                <span className="truncate">{t("writeEmail")}</span>
              </LinkButton>
              {agency.website && (
                <a
                  href={agency.website}
                  target="_blank"
                  rel="noopener noreferrer nofollow"
                  className="flex h-11 min-w-0 items-center justify-center gap-2 rounded-full border border-ink-100 bg-white px-4 text-sm font-medium text-ink-900 transition-colors hover:border-ink-200 hover:bg-ink-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600"
                >
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4 shrink-0" aria-hidden>
                    <circle cx="12" cy="12" r="9" />
                    <path d="M3 12h18M12 3c2.5 3 2.5 15 0 18M12 3c-2.5 3-2.5 15 0 18" />
                  </svg>
                  <span className="truncate">{websiteLabel(agency.website)}</span>
                  <span className="sr-only"> — {t("website")}</span>
                </a>
              )}
            </div>
          </div>
        </section>

        <section aria-labelledby="agency-listings" className="mt-10">
          <h2 id="agency-listings" className="font-display text-xl font-semibold text-ink-950">
            {t("listingsHeading")}
          </h2>

          {/* Sale/rent as tabs (links), then the type and the order — a GET form that submits itself. */}
          <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <nav aria-label={t("transactionFilter")} className="flex gap-1 rounded-full bg-ink-100/70 p-1 text-sm">
              {transactionTabs.map((tab) => {
                const active = state.transactionType === tab.value;
                return (
                  <Link
                    key={tab.label}
                    href={agencyListingsHref(agency.slug, { ...state, transactionType: tab.value, page: 1 })}
                    aria-current={active ? "page" : undefined}
                    className={cn(
                      "flex h-9 flex-1 items-center justify-center rounded-full px-4 font-medium transition-colors sm:flex-none",
                      active ? "bg-white text-ink-950 shadow-sm" : "text-ink-600 hover:text-ink-950",
                    )}
                  >
                    {tab.label}
                  </Link>
                );
              })}
            </nav>

            <AutoSubmitForm action={agencyPath(agency.slug)} className="grid grid-cols-2 gap-2 sm:flex sm:items-center">
              {state.transactionType && <input type="hidden" name="transactionType" value={state.transactionType} />}
              <SelectInput name="propertyType" defaultValue={state.propertyType ?? ""} aria-label={t("propertyTypeLabel")} className="h-10 text-sm sm:w-auto">
                <option value="">{t("allPropertyTypes")}</option>
                {PROPERTY_TYPES.map((type) => (
                  <option key={type} value={type}>
                    {tType(type)}
                  </option>
                ))}
              </SelectInput>
              <SelectInput name="sort" defaultValue={state.sort} aria-label={tSearch("sortLabel")} className="h-10 text-sm sm:w-auto">
                {AGENCY_LISTING_SORTS.map((sort) => (
                  <option key={sort} value={sort}>
                    {tSearch(`sort.${sort}`)}
                  </option>
                ))}
              </SelectInput>
              {/* Only needed without JavaScript; the form submits itself on change. */}
              <noscript>
                <Button type="submit" size="sm">
                  {t("apply")}
                </Button>
              </noscript>
            </AutoSubmitForm>
          </div>

          {listings === null ? (
            <p className="mt-6 text-sm text-ink-600">{t("listingsError")}</p>
          ) : listings.items.length > 0 ? (
            <>
              <div className="mt-5 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
                {listings.items.map((listing) => (
                  <PropertyCard key={listing.id} listing={listing} />
                ))}
              </div>
              <LinkPagination
                page={state.page}
                totalPages={totalPages}
                hrefFor={(page) => agencyListingsHref(agency.slug, { ...state, page })}
                labels={{ nav: tSearch("pagination"), previous: tSearch("previous"), next: tSearch("next") }}
              />
            </>
          ) : (
            <div className="mt-5 flex flex-col items-center gap-2 rounded-[18px] border border-dashed border-line bg-white px-6 py-14 text-center">
              <p className="font-semibold text-ink-900">{filtered ? t("noMatchingListings") : t("noListings")}</p>
              {filtered && (
                <Link href={agencyPath(agency.slug)} className="text-sm font-medium text-accent-700 hover:underline">
                  {t("showAllListings")}
                </Link>
              )}
            </div>
          )}
        </section>
      </main>
      <Footer />
    </div>
  );
}
