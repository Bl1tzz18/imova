import { redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { getSessionToken } from "@/lib/auth/session";
import { getMyPublishers } from "@/lib/api/publishers";
import { PropertyForm } from "./PropertyForm";

export default async function NewPropertyPage() {
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/properties/new");
  }

  const [t, publishers] = await Promise.all([getTranslations("NewPropertyPage"), getMyPublishers(token)]);

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <div className="mx-auto max-w-[1040px] px-4 py-10 sm:px-6 sm:py-14">
          <h1 className="font-display text-3xl font-medium text-ink-950 sm:text-4xl">{t("title")}</h1>
          <p className="mt-2 text-[15px] text-ink-500">{t("subtitle")}</p>

          <div className="mt-9">
            <PropertyForm publishers={publishers} />
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
}
