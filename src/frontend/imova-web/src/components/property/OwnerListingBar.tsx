import { getLocale, getTranslations } from "next-intl/server";
import { ListingStats } from "@/components/property/ListingStats";
import { OwnerListingActions } from "@/components/property/OwnerListingActions";
import { ownerActions, ownerNotice } from "@/lib/listing/ownerActions";
import { formatDate } from "@/lib/utils/format";
import { cn } from "@/lib/utils/cn";
import type { Listing } from "@/types/listing";

// Only the owner sees this, at the top of their own listing's page: whether visitors can see it
// (and until when), why not if they can't — with the moderators' reason — and the same actions as in
// "Anunțurile mele", so nothing needs a detour there.
export async function OwnerListingBar({ listing }: { listing: Listing }) {
  const [t, locale] = await Promise.all([getTranslations("OwnerListingBar"), getLocale()]);
  const now = new Date();
  const notice = ownerNotice(listing, now);

  const message =
    notice.kind === "active"
      ? notice.expiresAt
        ? t(notice.renewable ? "activeRenew" : "active", { date: formatDate(notice.expiresAt, locale) })
        : t("activeNoDate")
      : t(`${notice.kind}.${notice.status}`);

  return (
    <section
      aria-label={t("title")}
      className={cn(
        "mt-4 rounded-2xl border px-4 py-3.5 sm:px-5",
        notice.kind === "active" && "border-brand-100 bg-brand-50/60",
        notice.kind === "blocked" && "border-accent-100 bg-accent-100/40",
        (notice.kind === "notPublic" || notice.kind === "ended") && "border-ink-100 bg-white",
      )}
    >
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="min-w-0">
          <p className="text-xs font-semibold uppercase tracking-wide text-ink-500">{t("title")}</p>
          <p className="mt-0.5 text-sm font-medium text-ink-900">{message}</p>
          {/* How it's doing — once it has been public. */}
          {listing.publishedAt && <ListingStats listing={listing} className="mt-0.5" />}
          {notice.kind === "blocked" && notice.reason && (
            <p className="mt-1 text-sm text-ink-700">
              <span className="font-medium">{t("reasonLabel")}</span> {notice.reason}
            </p>
          )}
        </div>
        <OwnerListingActions listingId={listing.id} actions={ownerActions(listing, now)} className="shrink-0" />
      </div>
    </section>
  );
}
