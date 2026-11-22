import { getTranslations } from "next-intl/server";
import { AuthCard } from "@/components/auth/AuthCard";
import { LinkButton } from "@/components/ui/Button";

// Where deleting the account ends (from /account or the emailed link) — the session is already gone.
export default async function AccountDeletedPage() {
  const t = await getTranslations("Account");

  return (
    <AuthCard title={t("accountDeletedTitle")} subtitle={t("accountDeletedBody")}>
      <LinkButton href="/" className="w-full">
        {t("accountDeletedHome")}
      </LinkButton>
    </AuthCard>
  );
}
