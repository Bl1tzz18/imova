import { redirect } from "next/navigation";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { AccountSettings } from "@/components/account/AccountSettings";
import { EmailConfirmationBanner } from "@/components/auth/EmailConfirmationBanner";

export default async function AccountPage() {
  const profile = await getCurrentUserProfile();
  if (!profile) {
    redirect("/login?next=/account");
  }

  return (
    <main className="mx-auto max-w-4xl px-4 py-12 sm:px-6 sm:py-16">
      {!profile.emailConfirmed && <EmailConfirmationBanner email={profile.email} className="mb-8" />}
      <AccountSettings profile={profile} />
    </main>
  );
}
