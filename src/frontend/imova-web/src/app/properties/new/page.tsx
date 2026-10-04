import { redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { getSessionToken } from "@/lib/auth/session";
import { getMyAgencies } from "@/lib/api/agencies";
import { getMyPublishers } from "@/lib/api/publishers";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { EmailConfirmationBanner } from "@/components/auth/EmailConfirmationBanner";
import { PropertyForm } from "./PropertyForm";

// ?agencyId= starts "Publică ca" on that agency (when it's one of the user's).
export default async function NewPropertyPage({ searchParams }: { searchParams: Promise<{ agencyId?: string }> }) {
  const { agencyId } = await searchParams;
  const token = await getSessionToken();
  if (!token) {
    const back = agencyId ? `/properties/new?agencyId=${encodeURIComponent(agencyId)}` : "/properties/new";
    redirect(`/login?next=${encodeURIComponent(back)}`);
  }

  const [t, publishers, agencies, profile] = await Promise.all([
    getTranslations("NewPropertyPage"),
    getMyPublishers(token),
    getMyAgencies(token),
    getCurrentUserProfile(),
  ]);

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <div className="mx-auto max-w-[1040px] px-4 py-10 sm:px-6 sm:py-14">
          <h1 className="font-hero text-3xl font-extrabold text-ink-950 sm:text-4xl">{t("title")}</h1>
          <p className="mt-2 text-[15px] text-ink-500">{t("subtitle")}</p>

          {/* Heads-up before the form: without a confirmed email the listing is saved as a draft. */}
          {profile && !profile.emailConfirmed && <EmailConfirmationBanner email={profile.email} className="mt-6" />}

          <div className="mt-9">
            <PropertyForm publishers={publishers} agencies={agencies} requestedAgencyId={agencyId ?? null} />
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
}
