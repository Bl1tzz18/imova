import { getTranslations } from "next-intl/server";

export default async function CautaPage() {
  const t = await getTranslations("SearchPage");

  return (
    <main style={{ fontFamily: "sans-serif", padding: "2rem" }}>
      <h1>{t("title")}</h1>
    </main>
  );
}
