import { redirect } from "next/navigation";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getAccountDataSummary } from "@/lib/account/summary";
import { getEmailPreferences } from "@/lib/account/emailPreferences";
import { AccountSettings, type AccountTab } from "@/components/account/AccountSettings";
import { EmailConfirmationBanner } from "@/components/auth/EmailConfirmationBanner";

const TABS: readonly AccountTab[] = ["profile", "security", "notifications", "privacy"];

// ?tab=privacy opens "My data" directly (?tab=notifications: the email switches, linked from emails); ?export=tooSoon|failed is set by the data-export route
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
  const [summary, emailPreferences] = await Promise.all([getAccountDataSummary(), getEmailPreferences()]);

  return (
    <main className="mx-auto max-w-4xl px-4 py-12 sm:px-6 sm:py-16">
      {!profile.emailConfirmed && <EmailConfirmationBanner email={profile.email} className="mb-8" />}
      <AccountSettings
        profile={profile}
        summary={summary}
        emailPreferences={emailPreferences}
        initialTab={initialTab}
        exportStatus={exportStatus}
      />
    </main>
  );
}
