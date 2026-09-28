import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { AuthCard, AuthNotice } from "@/components/auth/AuthCard";
import { AuthTabs } from "@/components/auth/AuthTabs";
import { GoogleSignInButton } from "@/components/auth/GoogleSignInButton";
import { LoginForm } from "@/components/auth/LoginForm";
import { RegisterForm } from "@/components/auth/RegisterForm";

export async function AuthLayout({
  mode,
  next,
  notice,
}: {
  mode: "login" | "register";
  next?: string;
  // Shown above the form, e.g. "password changed" after a reset.
  notice?: string;
}) {
  const t = await getTranslations("Auth");

  return (
    <AuthCard
      header={<AuthTabs active={mode} next={next} />}
      title={mode === "login" ? t("loginTitle") : t("registerTitle")}
      subtitle={mode === "login" ? t("loginSubtitle") : t("registerSubtitle")}
      footer={
        <p className="mt-6 text-sm text-ink-500">
          {t("agencyPrompt")}{" "}
          <Link href="/properties/new" className="font-medium text-brand-700 hover:text-brand-800">
            {t("agencyCta")}
          </Link>
        </p>
      }
    >
      {notice && (
        <div className="mb-4">
          <AuthNotice tone="success">{notice}</AuthNotice>
        </div>
      )}
      {mode === "login" ? <LoginForm next={next} /> : <RegisterForm next={next} />}
      <GoogleSignInButton next={next} />
    </AuthCard>
  );
}
