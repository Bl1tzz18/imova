import { getTranslations } from "next-intl/server";
import { AuthLayout } from "@/components/auth/AuthLayout";

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ next?: string; reset?: string }>;
}) {
  const { next, reset } = await searchParams;
  const t = await getTranslations("Auth");

  // ?reset=1: resetPassword() lands here after a successful password reset.
  return <AuthLayout mode="login" next={next} notice={reset ? t("passwordResetDone") : undefined} />;
}
