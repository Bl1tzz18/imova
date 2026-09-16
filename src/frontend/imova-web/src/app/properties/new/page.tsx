import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { PropertyForm } from "./PropertyForm";

export default async function NewPropertyPage() {
  const t = await getTranslations("NewPropertyPage");

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <div className="mx-auto max-w-3xl px-4 py-10 sm:px-6">
          <h1 className="font-display text-3xl font-medium text-ink-950 sm:text-4xl">{t("title")}</h1>

          <div className="mt-8 rounded-2xl border border-ink-100 bg-white p-6 shadow-[var(--shadow-card)] sm:p-8">
            <PropertyForm />
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
}
