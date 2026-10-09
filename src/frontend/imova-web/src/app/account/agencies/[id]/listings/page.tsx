import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { LinkButton } from "@/components/ui/Button";
import { OwnerListingsList } from "@/components/property/OwnerListingsList";
import { getAgencyForMember, getAgencyListings } from "@/lib/api/agencies";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { canManage } from "@/lib/agency/manage";

export async function generateMetadata({ params }: { params: Promise<{ id: string }> }): Promise<Metadata> {
  const { id } = await params;
  const [agency, t] = await Promise.all([getAgencyForMember(id), getTranslations("AgencyManage")]);
  return { title: agency ? t("listingsPageTitle", { name: agency.name }) : undefined, robots: { index: false } };
}

// The Anunțuri tab: the agency's listings in every status, with the same tabs, search, sort and
// actions as "Anunțurile mele" (the listing actions already allow an agency's Owners/Admins). An
// Agent sees only their own. "Adaugă anunț" opens the form on this agency.
export default async function AgencyListingsPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ tab?: string; q?: string; sort?: string }>;
}) {
  const [{ id }, { tab, q, sort }] = await Promise.all([params, searchParams]);
  const [agency, profile, listings, t] = await Promise.all([
    getAgencyForMember(id),
    getCurrentUserProfile(),
    getAgencyListings(id),
    getTranslations("AgencyManage"),
  ]);
  if (!agency || !profile || !listings) notFound();

  const managesAll = canManage(agency.myRole, profile.roles.includes("Admin"));

  return (
    <div>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-ink-500">{managesAll ? t("listingsIntro") : t("listingsIntroAgent")}</p>
        {agency.status === "Active" && agency.myRole && (
          <LinkButton href={`/properties/new?agencyId=${encodeURIComponent(agency.id)}`} size="sm">
            {t("addListing")}
          </LinkButton>
        )}
      </div>
      <OwnerListingsList
        listings={listings}
        initialTab={tab}
        initialQuery={q ?? ""}
        initialSort={sort}
        showAuthor={managesAll}
        empty={{ title: t("listingsEmptyTitle"), body: t("listingsEmptyBody") }}
      />
    </div>
  );
}
