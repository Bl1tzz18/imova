import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { AgencyProfileForm } from "@/components/agency/manage/AgencyProfileForm";
import { EmailConfirmationBanner } from "@/components/auth/EmailConfirmationBanner";
import { getRaioane } from "@/lib/api/agencies";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { EMPTY_AGENCY_PROFILE, MY_AGENCIES_HREF, NEW_AGENCY_HREF } from "@/lib/agency/manage";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("AgencyManage");
  return { title: t("newTitle"), robots: { index: false } };
}

// Create an agency: the profile form (the logo comes right after, on the agency's page). The caller
// becomes its Owner. An unconfirmed email can't create one — the banner says why and resends the link.
export default async function NewAgencyPage() {
  const profile = await getCurrentUserProfile();
  if (!profile) {
    redirect(`/login?next=${encodeURIComponent(NEW_AGENCY_HREF)}`);
  }

  const [raioane, locale, t] = await Promise.all([getRaioane(), getLocale(), getTranslations("AgencyManage")]);

  return (
    <main className="mx-auto max-w-5xl px-4 py-10 sm:px-6 sm:py-12">
      <Link href={MY_AGENCIES_HREF} className="inline-flex items-center gap-1 text-sm font-medium text-ink-500 hover:text-ink-900">
        <span aria-hidden>←</span> {t("backToAgencies")}
      </Link>
      <h1 className="mt-4 font-display text-2xl font-medium text-ink-950 sm:text-[28px]">{t("newTitle")}</h1>
      <p className="mt-1.5 max-w-2xl text-sm text-ink-500">{t("newIntro")}</p>

      {!profile.emailConfirmed && (
        <div className="mt-6">
          <p className="mb-3 text-sm text-ink-700">{t("confirmEmailFirst")}</p>
          <EmailConfirmationBanner email={profile.email} />
        </div>
      )}

      <div className="mt-8">
        <AgencyProfileForm
          mode="create"
          initial={{ ...EMPTY_AGENCY_PROFILE, phone: profile.phoneNumber ?? "" }}
          raioane={raioane.map((r) => ({ id: r.id, label: locale === "ru" && r.nameRu ? r.nameRu : r.nameRo }))}
          accountEmail={profile.email}
        />
      </div>
    </main>
  );
}
