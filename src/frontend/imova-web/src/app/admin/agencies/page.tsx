import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { AgencyVerifyButton } from "@/components/admin/AgencyVerifyButton";
import { AutoSubmitForm } from "@/components/agency/AutoSubmitForm";
import { LinkPagination } from "@/components/agency/LinkPagination";
import { VerifiedBadge } from "@/components/agency/VerifiedBadge";
import { Avatar } from "@/components/ui/Avatar";
import { Badge } from "@/components/ui/Badge";
import { inputClass, SelectInput } from "@/components/ui/Field";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getSessionToken } from "@/lib/auth/session";
import { ADMIN_AGENCY_FILTERS, adminAgenciesApiQuery, adminAgenciesHref, parseAdminAgenciesParams } from "@/lib/admin/agencies";
import { agencyPath } from "@/lib/agency/publicPage";
import { formatDate } from "@/lib/utils/format";
import type { AdminAgenciesPage as AdminAgenciesResult } from "@/types/agency";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("AdminAgencies");
  return { title: t("title"), robots: { index: false } };
}

// Every agency for the admins, not yet verified first: search by name, filter by verification,
// verify / unverify. Only shows itself to admins; the API refuses everyone else at the route.
export default async function AdminAgenciesPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const token = await getSessionToken();
  if (!token) {
    redirect("/login?next=/admin/agencies");
  }

  const [profile, t, tSearch, locale, params] = await Promise.all([
    getCurrentUserProfile(),
    getTranslations("AdminAgencies"),
    getTranslations("Search"),
    getLocale(),
    searchParams,
  ]);
  if (!profile) {
    redirect("/login?next=/admin/agencies");
  }

  const state = parseAdminAgenciesParams(params);
  let result: AdminAgenciesResult | null = null;
  if (profile.roles.includes("Admin")) {
    const apiUrl = process.env.API_URL ?? "http://localhost:8080";
    const res = await fetch(`${apiUrl}/api/v1/admin/agencies?${adminAgenciesApiQuery(state)}`, {
      headers: { Authorization: `Bearer ${token}` },
      cache: "no-store",
    });
    // 403 here: the role was revoked but this login token doesn't know yet.
    result = res.ok ? await res.json() : null;
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / result.pageSize)) : 1;

  return (
    <div className="flex min-h-screen flex-col">
      <main className="mx-auto w-full max-w-4xl flex-1 px-4 py-10 sm:px-6">
        <h1 className="font-display text-2xl font-medium text-ink-950 sm:text-3xl">{t("title")}</h1>
        <p className="mt-1 text-sm text-ink-500">{t("subtitle")}</p>

        {result === null ? (
          <p className="mt-8 rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">{t("forbidden")}</p>
        ) : (
          <>
            <AutoSubmitForm action="/admin/agencies" className="mt-6 flex flex-col gap-2 sm:flex-row">
              <label className="min-w-0 flex-1">
                <span className="sr-only">{t("searchLabel")}</span>
                <input type="search" name="q" defaultValue={state.q} placeholder={t("searchPlaceholder")} className={inputClass} />
              </label>
              <label className="sm:w-56">
                <span className="sr-only">{t("filterLabel")}</span>
                <SelectInput name="show" defaultValue={state.show}>
                  {ADMIN_AGENCY_FILTERS.map((filter) => (
                    <option key={filter} value={filter}>
                      {t(`filter.${filter}`)}
                    </option>
                  ))}
                </SelectInput>
              </label>
            </AutoSubmitForm>

            <p className="mt-4 text-sm text-ink-500">{t("count", { count: result.totalCount })}</p>

            {result.items.length > 0 ? (
              <ul className="mt-3 divide-y divide-ink-100 overflow-hidden rounded-2xl border border-ink-100 bg-white">
                {result.items.map((agency) => (
                  <li key={agency.id} className="flex flex-col gap-3 px-4 py-4 sm:flex-row sm:items-center sm:px-5">
                    <div className="flex min-w-0 flex-1 items-start gap-3">
                      <Avatar
                        userId={agency.id}
                        displayName={agency.name}
                        pictureUrl={agency.logoThumbnailUrl}
                        size={44}
                        shape="square"
                        className="shrink-0 border border-ink-100"
                      />
                      <div className="min-w-0">
                        <p className="flex flex-wrap items-center gap-1.5">
                          <Link href={agencyPath(agency.slug)} className="min-w-0 truncate text-sm font-semibold text-ink-950 hover:underline">
                            {agency.name}
                          </Link>
                          {agency.isVerified && <VerifiedBadge />}
                          {agency.status === "Deactivated" && <Badge tone="accent">{t("deactivated")}</Badge>}
                        </p>
                        <p className="mt-0.5 text-xs text-ink-500">
                          {[
                            agency.raionName,
                            t("members", { count: agency.memberCount }),
                            t("activeListings", { count: agency.activeListingCount }),
                            t("createdOn", { date: formatDate(agency.createdAt, locale) }),
                          ]
                            .filter(Boolean)
                            .join(" · ")}
                        </p>
                        {agency.ownerEmail && (
                          <p className="mt-0.5 break-all text-xs text-ink-400">
                            {t("owner", { name: agency.ownerName ?? agency.ownerEmail })} · {agency.ownerEmail}
                          </p>
                        )}
                        {agency.isVerified && agency.verifiedAt && (
                          <p className="mt-0.5 text-xs text-brand-700">{t("verifiedOn", { date: formatDate(agency.verifiedAt, locale) })}</p>
                        )}
                      </div>
                    </div>
                    <div className="sm:shrink-0">
                      <AgencyVerifyButton agencyId={agency.id} agencyName={agency.name} verified={agency.isVerified} />
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="mt-3 rounded-2xl border border-dashed border-ink-200 bg-white px-6 py-10 text-center text-sm text-ink-600">
                {t("empty")}
              </p>
            )}

            <LinkPagination
              page={state.page}
              totalPages={totalPages}
              hrefFor={(page) => adminAgenciesHref({ ...state, page })}
              labels={{ nav: tSearch("pagination"), previous: tSearch("previous"), next: tSearch("next") }}
            />
          </>
        )}
      </main>
      <Footer />
    </div>
  );
}
