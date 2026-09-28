import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { AuthCard } from "@/components/auth/AuthCard";
import { ForgotPasswordForm } from "@/components/auth/ForgotPasswordForm";

export default async function ForgotPasswordPage({
  searchParams,
}: {
  searchParams: Promise<{ email?: string }>;
}) {
  const { email } = await searchParams;
  const t = await getTranslations("Auth");

  return (
    <AuthCard
      title={t("forgotPasswordTitle")}
      subtitle={t("forgotPasswordSubtitle")}
      footer={
        <p className="mt-6 text-sm text-ink-500">
          <Link href="/login" className="font-medium text-brand-700 hover:text-brand-800">
            {t("backToLogin")}
          </Link>
        </p>
      }
    >
      <ForgotPasswordForm defaultEmail={email} />
    </AuthCard>
  );
}
