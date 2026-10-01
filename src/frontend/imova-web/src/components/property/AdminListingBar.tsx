import Link from "next/link";
import { useTranslations } from "next-intl";
import { ListingStats } from "@/components/property/ListingStats";
import { moderationHref, type ListingTab } from "@/lib/admin/moderationTabs";
import type { Listing } from "@/types/listing";

const TAB_FOR_STATUS: Partial<Record<Listing["status"], ListingTab>> = {
  PendingReview: "pending",
  Active: "active",
  Suspended: "suspended",
};

// Only admins (not the owner — they have their own bar) see this on a listing's page: its number,
// views and how many people asked for the phone number, and a way to it in the moderation page.
export function AdminListingBar({ listing }: { listing: Listing }) {
  const t = useTranslations("ListingStats");
  const tab = TAB_FOR_STATUS[listing.status];

  return (
    <section
      aria-label={t("adminTitle")}
      className="mt-4 flex flex-wrap items-center justify-between gap-x-4 gap-y-1 rounded-2xl border border-dashed border-ink-200 bg-white px-4 py-3 sm:px-5"
    >
      <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
        <span className="rounded-full bg-ink-900 px-2 py-0.5 text-[11px] font-semibold uppercase tracking-wide text-white">
          {t("adminTitle")}
        </span>
        <ListingStats listing={listing} showNumber className="text-sm text-ink-700" />
      </div>
      {tab && (
        <Link href={moderationHref({ tab, q: listing.id })} className="text-sm font-medium text-brand-700 hover:underline">
          {t("openInModeration")}
        </Link>
      )}
    </section>
  );
}
