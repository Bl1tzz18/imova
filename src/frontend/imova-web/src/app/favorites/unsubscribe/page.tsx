import { getTranslations } from "next-intl/server";
import { AuthCard } from "@/components/auth/AuthCard";
import { LinkButton } from "@/components/ui/Button";
import { unsubscribeFavoriteAlerts } from "@/lib/account/emailPreferences";

// The "stop these emails" link in a saved-listing email: turns those emails off right away, no
// sign-in needed (like the saved-search one). The favorites themselves stay; the account's
// Notifications tab turns them back on.
export default async function UnsubscribeFavoriteAlertsPage({
  searchParams,
}: {
  searchParams: Promise<{ user?: string; token?: string }>;
}) {
  const { user, token } = await searchParams;
  const t = await getTranslations("FavoriteAlerts");
  const result = user && token ? await unsubscribeFavoriteAlerts(user, token) : "invalid";

  const [title, body] =
    result === "invalid"
      ? [t("unsubscribeInvalidTitle"), t("unsubscribeInvalidBody")]
      : result === "gone"
        ? [t("unsubscribeGoneTitle"), t("unsubscribeGoneBody")]
        : [t("unsubscribedTitle"), t("unsubscribedBody")];

  return (
    <AuthCard title={title} subtitle={body}>
      <LinkButton href="/account?tab=notifications" variant="secondary" className="w-full">
        {t("manage")}
      </LinkButton>
    </AuthCard>
  );
}
