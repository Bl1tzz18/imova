import { getTranslations } from "next-intl/server";
import { Badge } from "@/components/ui/Badge";
import { LinkButton } from "@/components/ui/Button";
import { BackLink } from "@/components/layout/BackLink";
import { SimilarListings } from "@/components/property/SimilarListings";
import { listingBackHref } from "@/lib/navigation/history";
import { searchHref } from "@/lib/search/filters";
import { formatLocation, formatPrice } from "@/lib/utils/format";
import type { EndedListing, Listing } from "@/types/listing";

// A link to a listing that is no longer up (sold, rented, expired, taken down): says so plainly and
// moves the visitor on — a search for the same kind of property nearby, and the similar listings —
// instead of a dead-end 404. Only what the API still gives out: no photos, description or contact.
export async function EndedListingView({ ended, similar }: { ended: EndedListing; similar: Listing[] }) {
  const [t, tType, tListing, tDetail] = await Promise.all([
    getTranslations("EndedListing"),
    getTranslations("PropertyType"),
    getTranslations("ListingType"),
    getTranslations("PropertyDetail"),
  ]);
  const location = formatLocation(ended);
  const kind = { transactionType: ended.transactionType, propertyType: ended.propertyType };

  return (
    <main className="flex-1">
      <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
        <BackLink href={listingBackHref(null, kind)} label={tDetail("back")} toLastSearch />

        <section className="mt-5 max-w-2xl rounded-2xl border border-ink-100 bg-white p-6 shadow-[var(--shadow-card)] sm:p-8">
          <div className="flex flex-wrap items-center gap-2">
            <Badge tone="accent">{t(`status.${ended.status}`)}</Badge>
            <Badge tone="neutral">{tListing(ended.transactionType)}</Badge>
            <Badge tone="neutral">{tType(ended.propertyType)}</Badge>
          </div>

          <h1 className="mt-4 text-balance font-display text-2xl font-medium text-ink-950 sm:text-3xl">{ended.title}</h1>
          {location && <p className="mt-1.5 text-sm text-ink-500">{location}</p>}

          <div className="mt-5 flex items-start gap-3 rounded-xl bg-ink-50 px-4 py-3.5">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" className="mt-0.5 h-5 w-5 shrink-0 text-ink-500" aria-hidden>
              <circle cx="12" cy="12" r="9" />
              <path d="M12 8v4M12 15.5h.01" strokeLinecap="round" />
            </svg>
            <div>
              <p className="font-medium text-ink-900">{t("title")}</p>
              <p className="mt-0.5 text-sm text-ink-600">{t(`reason.${ended.status}`)}</p>
            </div>
          </div>

          <p className="mt-4 text-sm text-ink-600">
            {t("lastPrice")}{" "}
            <span className="font-semibold text-ink-900">{formatPrice(ended.price.amount, ended.price.currency)}</span>
            {ended.totalAreaM2 > 0 && <span className="text-ink-500"> · {ended.totalAreaM2} m²</span>}
          </p>

          <LinkButton
            href={searchHref({ transactionType: [ended.transactionType], propertyType: [ended.propertyType], raionId: [ended.raionId] })}
            className="mt-6 w-full sm:w-auto"
          >
            {t("searchSimilar")}
          </LinkButton>
        </section>

        <SimilarListings listings={similar} title={tDetail("similarListings")} />
      </div>
    </main>
  );
}
