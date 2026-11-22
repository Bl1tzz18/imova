import { getTranslations } from "next-intl/server";
import { AuthCard, AuthNotice } from "@/components/auth/AuthCard";
import { ResetPasswordForm } from "@/components/auth/ResetPasswordForm";
import { LinkButton } from "@/components/ui/Button";

// Reached from the password-reset email (AccountEmails on the backend).
export default async function ResetPasswordPage({
  searchParams,
}: {
  searchParams: Promise<{ email?: string; token?: string }>;
}) {
  const { email, token } = await searchParams;
  const t = await getTranslations("Auth");

  if (!email || !token) {
    return (
      <AuthCard title={t("resetPasswordTitle")}>
        <div className="flex flex-col gap-4">
          <AuthNotice tone="error">{t("resetLinkInvalid")}</AuthNotice>
          <LinkButton href="/forgot-password" className="w-full">
            {t("requestNewLink")}
          </LinkButton>
        </div>
      </AuthCard>
    );
  }

  return (
    <AuthCard title={t("resetPasswordTitle")} subtitle={t("resetPasswordSubtitle", { email })}>
      <ResetPasswordForm email={email} token={token} />
    </AuthCard>
  );
}
