import { getTranslations } from "next-intl/server";

export default async function ProprietatePage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const t = await getTranslations("PropertyPage");

  return (
    <main style={{ fontFamily: "sans-serif", padding: "2rem" }}>
      <h1>{t("title", { slug })}</h1>
    </main>
  );
}
