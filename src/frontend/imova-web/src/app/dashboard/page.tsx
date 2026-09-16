import { getTranslations } from "next-intl/server";

export default async function DashboardPage() {
  const t = await getTranslations("DashboardPage");

  return (
    <main style={{ fontFamily: "sans-serif", padding: "2rem" }}>
      <h1>{t("title")}</h1>
    </main>
  );
}
