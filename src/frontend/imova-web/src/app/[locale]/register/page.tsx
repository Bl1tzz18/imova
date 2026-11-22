import { redirectTo } from "@/i18n/redirect";
import { AuthLayout } from "@/components/auth/AuthLayout";
import { safeNextPath } from "@/lib/auth/nextPath";
import { getCurrentUserProfile } from "@/lib/auth/profile";

export default async function RegisterPage({
  searchParams,
}: {
  searchParams: Promise<{ next?: string }>;
}) {
  const { next } = await searchParams;

  // Already signed in: same as /login — go where signing up would have led.
  if (await getCurrentUserProfile()) {
    return redirectTo(safeNextPath(next));
  }

  return <AuthLayout mode="register" next={next} />;
}
