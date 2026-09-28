import { getTranslations } from "next-intl/server";
import { AuthCard, AuthNotice } from "@/components/auth/AuthCard";
import { EmailConfirmationBanner } from "@/components/auth/EmailConfirmationBanner";
import { LinkButton } from "@/components/ui/Button";
import { confirmEmail } from "@/lib/auth/actions";
import { getCurrentUserProfile } from "@/lib/auth/profile";

// Reached from the confirmation email (AccountEmails on the backend). Opening the link is the
// confirmation — there's nothing to click; any listings waiting as drafts go to review with it.
export default async function ConfirmEmailPage({
  searchParams,
}: {
  searchParams: Promise<{ userId?: string; token?: string }>;
}) {
  const { userId, token } = await searchParams;
  const t = await getTranslations("Auth");
  const result = userId && token ? await confirmEmail(userId, token) : { error: t("confirmLinkInvalid") };

  if (result.success) {
    return (
      <AuthCard title={t("confirmSuccessTitle")} subtitle={t("confirmSuccessBody")}>
        <div className="flex flex-col gap-3">
          <LinkButton href="/my-listings" className="w-full">
            {t("confirmSuccessMyListings")}
          </LinkButton>
          <LinkButton href="/" variant="secondary" className="w-full">
            {t("confirmSuccessHome")}
          </LinkButton>
        </div>
      </AuthCard>
    );
  }

  // Signed in (and still unconfirmed): offer a fresh link right here.
  const profile = await getCurrentUserProfile();

  return (
    <AuthCard title={t("confirmFailedTitle")}>
      <div className="flex flex-col gap-4">
        <AuthNotice tone="error">{result.error}</AuthNotice>
        {profile && !profile.emailConfirmed ? (
          <EmailConfirmationBanner email={profile.email} />
        ) : (
          !profile && (
            <LinkButton href="/login?next=/account" className="w-full">
              {t("confirmFailedSignIn")}
            </LinkButton>
          )
        )}
      </div>
    </AuthCard>
  );
}
