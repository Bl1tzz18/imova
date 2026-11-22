import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { AgencyLogoUploader } from "@/components/agency/manage/AgencyLogoUploader";
import { AgencyProfileForm } from "@/components/agency/manage/AgencyProfileForm";
import { getAgencyForMember, getRaioane } from "@/lib/api/agencies";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { agencyProfileValues, canManage } from "@/lib/agency/manage";
import { formatPhone } from "@/lib/listing/contactCard";

export async function generateMetadata({ params }: { params: Promise<{ id: string }> }): Promise<Metadata> {
  const { id } = await params;
  const [agency, t] = await Promise.all([getAgencyForMember(id), getTranslations("AgencyManage")]);
  return { title: agency ? t("pageTitle", { name: agency.name }) : t("newTitle"), robots: { index: false } };
}

// The Profil tab: logo and profile for Owners/Admins (and site admins), the details as text for
// Agents. ?created=1 right after creating it: say so, and ask for a logo.
export default async function AgencyProfilePage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ created?: string }>;
}) {
  const [{ id }, { created }] = await Promise.all([params, searchParams]);
  const [agency, profile, raioane, locale, t] = await Promise.all([
    getAgencyForMember(id),
    getCurrentUserProfile(),
    getRaioane(),
    getLocale(),
    getTranslations("AgencyManage"),
  ]);
  if (!agency || !profile) notFound();

  const editable = canManage(agency.myRole, profile.roles.includes("Admin"));
  const justCreated = created === "1";

  if (!editable) {
    const rows: [string, string | null][] = [
      [t("phoneLabel"), agency.phone ? formatPhone(agency.phone) : null],
      [t("emailLabel"), agency.email],
      [t("websiteLabel"), agency.website],
      [t("cityLabel"), agency.raionName],
      [t("addressLabel"), agency.address],
    ];
    return (
      <div className="flex flex-col gap-4">
        <p className="rounded-xl bg-ink-50 px-4 py-3 text-sm text-ink-600">{t("agentReadOnly")}</p>
        <dl className="grid gap-x-6 gap-y-3 rounded-2xl border border-ink-100 p-4 sm:grid-cols-[10rem_1fr] sm:p-5">
          {rows
            .filter(([, value]) => value)
            .map(([label, value]) => (
              <div key={label} className="contents">
                <dt className="text-sm text-ink-500">{label}</dt>
                <dd className="min-w-0 break-words text-sm text-ink-900">{value}</dd>
              </div>
            ))}
        </dl>
        {agency.bio && <p className="whitespace-pre-line text-sm text-ink-700">{agency.bio}</p>}
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      {justCreated && (
        <p role="status" className="rounded-xl border border-brand-100 bg-brand-100/60 px-4 py-3 text-sm text-brand-700">
          {t("createdNotice", { name: agency.name })}
        </p>
      )}

      <AgencyLogoUploader agencyId={agency.id} name={agency.name} logoUrl={agency.logoUrl} highlight={justCreated} />

      <AgencyProfileForm
        mode="edit"
        agencyId={agency.id}
        initial={agencyProfileValues(agency)}
        raioane={raioane.map((r) => ({ id: r.id, label: locale === "ru" && r.nameRu ? r.nameRu : r.nameRo }))}
        logoUrl={agency.logoThumbnailUrl}
        isVerified={agency.isVerified}
      />
    </div>
  );
}
