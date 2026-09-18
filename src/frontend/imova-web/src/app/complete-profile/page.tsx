import { redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { CompleteProfileForm } from "@/components/auth/CompleteProfileForm";
import { getSessionToken } from "@/lib/auth/session";

export default async function CompleteProfilePage({
  searchParams,
}: {
  searchParams: Promise<{ next?: string }>;
}) {
  const { next } = await searchParams;

  // Only reachable while signed in — googleLogin() redirects here right after setting the
  // session cookie for a brand-new Google account with no phone number on file yet.
  const token = await getSessionToken();
  if (!token) {
    redirect("/login");
  }

  const t = await getTranslations("Auth");

  return (
    <main className="relative flex min-h-[calc(100vh-5rem)] flex-col items-center overflow-hidden px-4 py-16 sm:px-6">
      <div className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-[480px] bg-[radial-gradient(55%_55%_at_50%_0%,var(--color-brand-100),transparent_70%)]" />

      <div className="w-full max-w-[400px] rounded-2xl border border-ink-100 bg-white p-8 shadow-[var(--shadow-card)]">
        <h1 className="font-display text-2xl font-medium text-ink-950">{t("completeProfileTitle")}</h1>
        <p className="mt-1.5 text-sm text-ink-500">{t("completeProfileSubtitle")}</p>
        <div className="mt-6">
          <CompleteProfileForm next={next} />
        </div>
      </div>
    </main>
  );
}
