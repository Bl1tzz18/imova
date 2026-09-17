import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { AuthTabs } from "@/components/auth/AuthTabs";
import { GoogleSignInButton } from "@/components/auth/GoogleSignInButton";
import { LoginForm } from "@/components/auth/LoginForm";
import { RegisterForm } from "@/components/auth/RegisterForm";

export async function AuthLayout({ mode, next }: { mode: "login" | "register"; next?: string }) {
  const t = await getTranslations("Auth");

  return (
    <main className="relative flex min-h-[calc(100vh-5rem)] flex-col items-center overflow-hidden px-4 py-16 sm:px-6">
      <div className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-[480px] bg-[radial-gradient(55%_55%_at_50%_0%,var(--color-brand-100),transparent_70%)]" />

      <div className="w-full max-w-[400px] rounded-2xl border border-ink-100 bg-white p-8 shadow-[var(--shadow-card)]">
        <AuthTabs active={mode} next={next} />

        {mode === "login" ? (
          <>
            <h1 className="font-display text-2xl font-medium text-ink-950">{t("loginTitle")}</h1>
            <p className="mt-1.5 text-sm text-ink-500">{t("loginSubtitle")}</p>
            <div className="mt-6">
              <LoginForm next={next} />
            </div>
          </>
        ) : (
          <>
            <h1 className="font-display text-2xl font-medium text-ink-950">{t("registerTitle")}</h1>
            <p className="mt-1.5 text-sm text-ink-500">{t("registerSubtitle")}</p>
            <div className="mt-6">
              <RegisterForm next={next} />
            </div>
          </>
        )}

        <GoogleSignInButton next={next} />
      </div>

      <p className="mt-6 text-sm text-ink-500">
        {t("agencyPrompt")}{" "}
        <Link href="/properties/new" className="font-medium text-brand-700 hover:text-brand-800">
          {t("agencyCta")}
        </Link>
      </p>
    </main>
  );
}
