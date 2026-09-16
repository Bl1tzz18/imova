import { getTranslations } from "next-intl/server";
import { PropertyForm } from "./PropertyForm";

export default async function NewPropertyPage() {
  const t = await getTranslations("NewPropertyPage");

  return (
    <main style={{ fontFamily: "sans-serif", padding: "2rem" }}>
      <h1>{t("title")}</h1>
      <PropertyForm />
    </main>
  );
}
