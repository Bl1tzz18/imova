import { redirectTo } from "@/i18n/redirect";
import { getTranslations } from "next-intl/server";
import { AuthCard } from "@/components/auth/AuthCard";
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
    return redirectTo("/login");
  }

  const t = await getTranslations("Auth");

  return (
    <AuthCard title={t("completeProfileTitle")} subtitle={t("completeProfileSubtitle")}>
      <CompleteProfileForm next={next} />
    </AuthCard>
  );
}
