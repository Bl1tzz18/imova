import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { LinkButton } from "@/components/ui/Button";

// Any address that isn't a page, in the visitor's language (see [...rest]/page.tsx).
export default async function NotFound() {
  const t = await getTranslations("Meta");

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex flex-1 items-center justify-center px-4 py-24 text-center">
        <div className="flex flex-col items-center gap-3">
          <h1 className="font-display text-2xl font-medium text-ink-950">{t("notFoundTitle")}</h1>
          <p className="max-w-sm text-sm text-ink-500">{t("notFoundText")}</p>
          <LinkButton href="/" size="sm" className="mt-2">
            {t("backHome")}
          </LinkButton>
        </div>
      </main>
      <Footer />
    </div>
  );
}
