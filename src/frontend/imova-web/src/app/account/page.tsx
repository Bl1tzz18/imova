import { redirect } from "next/navigation";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getAccountDataSummary } from "@/lib/account/summary";
import { getEmailPreferences } from "@/lib/account/emailPreferences";
import { getMyAgencies, getMyInvitations } from "@/lib/api/agencies";
import { getSessionToken } from "@/lib/auth/session";
import { AccountSettings, type AccountTab } from "@/components/account/AccountSettings";
import { EmailConfirmationBanner } from "@/components/auth/EmailConfirmationBanner";

const TABS: readonly AccountTab[] = ["profile", "agencies", "security", "notifications", "privacy"];

// ?tab=privacy opens "My data" directly (?tab=notifications: the email switches, linked from emails;
// ?tab=agencies: "Agențiile mele"); ?export=tooSoon|failed is set by the data-export route
// when a download couldn't be made.
export default async function AccountPage({
  searchParams,
}: {
  searchParams: Promise<{ tab?: string; export?: string }>;
}) {
  const profile = await getCurrentUserProfile();
  if (!profile) {
    redirect("/login?next=/account");
  }

  const params = await searchParams;
  const initialTab = TABS.find((tab) => tab === params.tab) ?? "profile";
  const exportStatus = params.export === "tooSoon" || params.export === "failed" ? params.export : undefined;
  const token = await getSessionToken();
  const [summary, emailPreferences, agencies, invitations] = await Promise.all([
    getAccountDataSummary(),
    getEmailPreferences(),
    // A failure only empties that tab (it says the list couldn't be loaded).
    token ? getMyAgencies(token).catch(() => null) : null,
    token ? getMyInvitations(token).catch(() => null) : null,
  ]);

  return (
    <main className="mx-auto max-w-4xl px-4 py-12 sm:px-6 sm:py-16">
      {!profile.emailConfirmed && <EmailConfirmationBanner email={profile.email} className="mb-8" />}
      <AccountSettings
        profile={profile}
        summary={summary}
        emailPreferences={emailPreferences}
        agencies={agencies}
        invitations={invitations}
        initialTab={initialTab}
        exportStatus={exportStatus}
      />
    </main>
  );
}
