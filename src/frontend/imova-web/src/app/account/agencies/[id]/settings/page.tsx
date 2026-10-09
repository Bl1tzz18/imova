import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { CopyLinkButton, LeaveAgencySection } from "@/components/agency/manage/AgencySettingsActions";
import { getAgencyForMember, getAgencyMembers } from "@/lib/api/agencies";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { agencyPath } from "@/lib/agency/publicPage";
import { siteUrl } from "@/lib/site";

export async function generateMetadata({ params }: { params: Promise<{ id: string }> }): Promise<Metadata> {
  const { id } = await params;
  const [agency, t] = await Promise.all([getAgencyForMember(id), getTranslations("AgencyManage")]);
  return { title: agency ? t("settingsPageTitle", { name: agency.name }) : undefined, robots: { index: false } };
}

// The Setări tab: the public page's address to share, and leaving the agency. (Deactivating and
// deleting the agency come with the next step.)
export default async function AgencySettingsPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const [agency, profile, members, t] = await Promise.all([
    getAgencyForMember(id),
    getCurrentUserProfile(),
    getAgencyMembers(id),
    getTranslations("AgencyManage"),
  ]);
  if (!agency || !profile || !members) notFound();

  return (
    <div className="flex flex-col gap-6">
      <section aria-labelledby="agency-public-page-title" className="rounded-2xl border border-ink-100 p-4 sm:p-5">
        <h2 id="agency-public-page-title" className="font-display text-lg font-medium text-ink-950">
          {t("publicPageTitle")}
        </h2>
        {agency.status === "Active" ? (
          <>
            <p className="mb-4 mt-1 text-sm text-ink-500">{t("publicPageIntro")}</p>
            <CopyLinkButton url={`${siteUrl()}${agencyPath(agency.slug)}`} />
          </>
        ) : (
          <p className="mt-1 text-sm text-ink-500">{t("publicPageHidden")}</p>
        )}
      </section>

      {agency.myRole && (
        <LeaveAgencySection agencyId={agency.id} agencyName={agency.name} userId={profile.id} members={members} />
      )}
    </div>
  );
}
