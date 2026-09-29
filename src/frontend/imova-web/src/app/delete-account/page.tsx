import { getTranslations } from "next-intl/server";
import { AuthCard, AuthNotice } from "@/components/auth/AuthCard";
import { ConfirmAccountDeletionForm } from "@/components/account/ConfirmAccountDeletionForm";
import { LinkButton } from "@/components/ui/Button";

// Reached from the "confirm deleting your account" email (accounts without a password). Unlike
// /confirm-email, opening the link does nothing by itself — mail scanners open links; the account
// is only deleted when the button on this page is pressed.
export default async function DeleteAccountPage({
  searchParams,
}: {
  searchParams: Promise<{ userId?: string; token?: string }>;
}) {
  const { userId, token } = await searchParams;
  const t = await getTranslations("Account");

  if (!userId || !token) {
    return (
      <AuthCard title={t("deleteConfirmPageTitle")}>
        <div className="flex flex-col gap-4">
          <AuthNotice tone="error">{t("deleteLinkInvalid")}</AuthNotice>
          <LinkButton href="/account?tab=privacy" variant="secondary" className="w-full">
            {t("deleteBackToAccount")}
          </LinkButton>
        </div>
      </AuthCard>
    );
  }

  return (
    <AuthCard title={t("deleteConfirmPageTitle")} subtitle={t("deleteConfirmPageBody")}>
      <ConfirmAccountDeletionForm userId={userId} token={token} />
    </AuthCard>
  );
}
