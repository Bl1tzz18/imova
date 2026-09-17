import { AuthLayout } from "@/components/auth/AuthLayout";

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ next?: string }>;
}) {
  const { next } = await searchParams;
  return <AuthLayout mode="login" next={next} />;
}
