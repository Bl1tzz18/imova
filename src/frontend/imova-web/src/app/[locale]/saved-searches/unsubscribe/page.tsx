import { getTranslations } from "next-intl/server";
import { AuthCard } from "@/components/auth/AuthCard";
import { LinkButton } from "@/components/ui/Button";
import { unsubscribeSavedSearch } from "@/lib/savedSearches/api";
import { SAVED_SEARCHES_PATH } from "@/lib/savedSearches/savedSearch";

// The "stop these emails" link in a saved-search alert: turns that search's alerts off right away,
// no sign-in needed. The search itself stays saved.
export default async function UnsubscribeSavedSearchPage({
  searchParams,
}: {
  searchParams: Promise<{ id?: string; token?: string }>;
}) {
  const { id, token } = await searchParams;
  const t = await getTranslations("SavedSearches");
  const result = id && token ? await unsubscribeSavedSearch(id, token) : "invalid";

  const [title, body] =
    result === "invalid"
      ? [t("unsubscribeInvalidTitle"), t("unsubscribeInvalidBody")]
      : result === "gone"
        ? [t("unsubscribeGoneTitle"), t("unsubscribeGoneBody")]
        : [t("unsubscribedTitle"), t("unsubscribedBody", { name: result.name })];

  return (
    <AuthCard title={title} subtitle={body}>
      <LinkButton href={SAVED_SEARCHES_PATH} variant="secondary" className="w-full">
        {t("manage")}
      </LinkButton>
    </AuthCard>
  );
}
