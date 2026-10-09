import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { Avatar } from "@/components/ui/Avatar";
import { Badge } from "@/components/ui/Badge";
import { VerifiedBadge } from "@/components/agency/VerifiedBadge";
import { AgencyTabs } from "@/components/agency/manage/AgencyTabs";
import { getAgencyForMember } from "@/lib/api/agencies";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { agencyManagePath, MY_AGENCIES_HREF } from "@/lib/agency/manage";
import { agencyPath } from "@/lib/agency/publicPage";

// The agency's management pages: its header (logo, name, the caller's role, status), the four tabs,
// then the tab itself. Members only (and site admins); anyone else gets a 404, like for an agency
// they can't see.
export default async function AgencyManageLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const profile = await getCurrentUserProfile();
  if (!profile) {
    redirect(`/login?next=${encodeURIComponent(agencyManagePath(id))}`);
  }

  const agency = await getAgencyForMember(id);
  const isSiteAdmin = profile.roles.includes("Admin");
  if (!agency || (!agency.myRole && !isSiteAdmin)) {
    notFound();
  }

  const t = await getTranslations("AgencyManage");

  return (
    <main className="mx-auto max-w-5xl px-4 py-10 sm:px-6 sm:py-12">
      <Link href={MY_AGENCIES_HREF} className="inline-flex items-center gap-1 text-sm font-medium text-ink-500 hover:text-ink-900">
        <span aria-hidden>←</span> {t("backToAgencies")}
      </Link>

      <header className="mt-4 flex items-center gap-4">
        <Avatar
          userId={agency.id}
          displayName={agency.name}
          pictureUrl={agency.logoThumbnailUrl}
          size={56}
          shape="square"
          className="shrink-0 border border-ink-100"
        />
        <div className="min-w-0 flex-1">
          <h1 className="flex items-center gap-2 font-display text-xl font-medium text-ink-950 sm:text-2xl">
            <span className="truncate">{agency.name}</span>
            {agency.isVerified && <VerifiedBadge />}
          </h1>
          <p className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-ink-500">
            {agency.myRole && <Badge tone={agency.myRole === "Agent" ? "neutral" : "brand"}>{t(`role.${agency.myRole}`)}</Badge>}
            {agency.status === "Deactivated" ? (
              <Badge tone="accent">{t("deactivated")}</Badge>
            ) : (
              <Link href={agencyPath(agency.slug)} className="font-medium text-brand-700 hover:underline">
                {t("viewPublicPage")}
              </Link>
            )}
          </p>
        </div>
      </header>

      <div className="mt-6">
        <AgencyTabs agencyId={agency.id} />
      </div>

      <div className="mt-6">{children}</div>
    </main>
  );
}
