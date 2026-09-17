import { AuthLayout } from "@/components/auth/AuthLayout";

export default async function RegisterPage({
  searchParams,
}: {
  searchParams: Promise<{ next?: string }>;
}) {
  const { next } = await searchParams;
  return <AuthLayout mode="register" next={next} />;
}
