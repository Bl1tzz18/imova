import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { MembersPanel } from "@/components/agency/manage/MembersPanel";
import { getAgencyForMember, getAgencyInvitations, getAgencyMembers } from "@/lib/api/agencies";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { canManage } from "@/lib/agency/manage";

export async function generateMetadata({ params }: { params: Promise<{ id: string }> }): Promise<Metadata> {
  const { id } = await params;
  const [agency, t] = await Promise.all([getAgencyForMember(id), getTranslations("AgencyManage")]);
  return { title: agency ? t("membersPageTitle", { name: agency.name }) : undefined, robots: { index: false } };
}

// The Membri tab. The open invitations only for those who manage members.
export default async function AgencyMembersPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const [agency, profile] = await Promise.all([getAgencyForMember(id), getCurrentUserProfile()]);
  if (!agency || !profile) notFound();

  const isSiteAdmin = profile.roles.includes("Admin");
  const [members, invitations] = await Promise.all([
    getAgencyMembers(id),
    canManage(agency.myRole, isSiteAdmin) ? getAgencyInvitations(id) : Promise.resolve(null),
  ]);
  if (!members) notFound();

  return (
    <MembersPanel
      agencyId={agency.id}
      members={members}
      invitations={invitations}
      actor={{ userId: profile.id, role: agency.myRole, isSiteAdmin }}
    />
  );
}
