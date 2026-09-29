import { redirect } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { GrantAdminForm } from "@/components/admin/GrantAdminForm";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getSessionToken } from "@/lib/auth/session";

type AdminUser = { id: string; email: string; displayName: string | null; grantedAt: string | null; grantedBy: string | null };

// Who the admins are, and adding one. The page only shows itself to admins; the API refuses
// everyone else regardless (401/403 at the route, then a database role check).
export default async function AdminAdminsPage() {
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/admins");
  }

  const [profile, t, locale] = await Promise.all([getCurrentUserProfile(), getTranslations("AdminAdminsPage"), getLocale()]);
  if (!profile) {
    redirect("/login?next=/admin/admins");
  }

  if (!profile.roles.includes("Admin")) {
    return (
      <div className="flex min-h-screen flex-col">
        <main className="mx-auto w-full max-w-2xl flex-1 px-4 py-16 text-center sm:px-6">
          <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">{t("forbidden")}</p>
        </main>
        <Footer />
      </div>
    );
  }

  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/admin/admins`, { headers: { Authorization: `Bearer ${token}` }, cache: "no-store" });
  // 403 here: the role was revoked but this login token doesn't know yet.
  const admins: AdminUser[] | null = res.ok ? await res.json() : null;
  const dateFormat = new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "short", timeZone: "Europe/Chisinau" });

  return (
    <div className="flex min-h-screen flex-col">
      <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-10 sm:px-6">
        <h1 className="font-display text-2xl font-medium text-ink-950 sm:text-3xl">{t("title")}</h1>
        <p className="mt-1 text-sm text-ink-500">{t("subtitle")}</p>

        {admins === null ? (
          <p className="mt-8 rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">{t("forbidden")}</p>
        ) : (
          <>
            <ul className="mt-8 divide-y divide-ink-100 overflow-hidden rounded-2xl border border-ink-100 bg-white">
              {admins.map((admin) => (
                <li key={admin.id} className="flex flex-col gap-0.5 px-5 py-3.5 sm:flex-row sm:items-center sm:justify-between">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-ink-900">
                      {admin.displayName ?? admin.email}
                      {admin.id === profile.id && <span className="ml-2 text-xs font-normal text-ink-400">{t("you")}</span>}
                    </p>
                    <p className="truncate text-xs text-ink-500">{admin.email}</p>
                  </div>
                  <p className="text-xs text-ink-400">
                    {admin.grantedAt
                      ? t("grantedBy", { by: admin.grantedBy ?? t("deletedAccount"), date: dateFormat.format(new Date(admin.grantedAt)) })
                      : t("grantedBeforeAudit")}
                  </p>
                </li>
              ))}
            </ul>

            <section className="mt-10 rounded-2xl border border-ink-100 bg-white p-6 sm:p-8">
              <h2 className="text-base font-semibold text-ink-950">{t("addTitle")}</h2>
              <p className="mt-1.5 text-sm text-ink-500">{t("addBody")}</p>
              <div className="mt-5">
                <GrantAdminForm />
              </div>
            </section>
          </>
        )}
      </main>
      <Footer />
    </div>
  );
}
