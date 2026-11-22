import { redirectTo } from "@/i18n/redirect";
import { getTranslations } from "next-intl/server";
import { AuthLayout } from "@/components/auth/AuthLayout";
import { safeNextPath } from "@/lib/auth/nextPath";
import { getCurrentUserProfile } from "@/lib/auth/profile";

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ next?: string; reset?: string }>;
}) {
  const { next, reset } = await searchParams;

  // Already signed in: nothing to do here — go where the sign-in would have led. The profile call
  // (not just the cookie) decides, so a session that has ended still shows the form.
  if (await getCurrentUserProfile()) {
    return redirectTo(safeNextPath(next));
  }

  const t = await getTranslations("Auth");

  // ?reset=1: resetPassword() lands here after a successful password reset.
  return <AuthLayout mode="login" next={next} notice={reset ? t("passwordResetDone") : undefined} />;
}
