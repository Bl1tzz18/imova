import { redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { ModerationQueue } from "@/components/admin/ModerationQueue";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getSessionToken } from "@/lib/auth/session";
import type { Property } from "@/types/property";

type PagedResult<T> = { items: T[]; page: number; pageSize: number; totalCount: number };

async function getPendingReviewProperties(token: string): Promise<PagedResult<Property>> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/admin/properties/pending-review?page=1&pageSize=50`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch moderation queue: ${res.status}`);
  }

  return res.json();
}

export default async function AdminModerationPage() {
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/moderation");
  }

  const [profile, t] = await Promise.all([
    getCurrentUserProfile(),
    getTranslations("AdminModerationPage"),
  ]);

  if (!profile) {
    redirect("/login?next=/admin/moderation");
  }

  if (!profile.roles.includes("Admin")) {
    return (
      <div className="flex min-h-screen flex-col">
        <main className="flex-1">
          <div className="mx-auto max-w-2xl px-4 py-16 text-center sm:px-6">
            <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
              {t("forbiddenText")}
            </p>
          </div>
        </main>
        <Footer />
      </div>
    );
  }

  const result = await getPendingReviewProperties(token);

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <div className="mx-auto max-w-5xl px-4 py-10 sm:px-6">
          <h1 className="font-display text-2xl font-medium text-ink-950 sm:text-3xl">{t("title")}</h1>
          <p className="mt-1 text-sm text-ink-500">
            {result.totalCount > 0 ? t("resultsCount", { count: result.totalCount }) : t("emptyTitle")}
          </p>

          <div className="mt-8">
            {result.items.length > 0 ? (
              <ModerationQueue properties={result.items} />
            ) : (
              <div className="flex flex-col items-center gap-2 rounded-2xl border border-dashed border-ink-200 bg-white px-6 py-16 text-center">
                <p className="text-sm font-medium text-ink-700">{t("emptyTitle")}</p>
                <p className="text-sm text-ink-500">{t("emptyBody")}</p>
              </div>
            )}
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
}
