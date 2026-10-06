import type { Metadata } from "next";
import Link from "next/link";
import { getLocale, getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { AutoSubmitForm } from "@/components/agency/AutoSubmitForm";
import { LinkPagination } from "@/components/agency/LinkPagination";
import { VerifiedBadge } from "@/components/agency/VerifiedBadge";
import { Avatar } from "@/components/ui/Avatar";
import { Button } from "@/components/ui/Button";
import { SelectInput, TextInput } from "@/components/ui/Field";
import { getAgencyDirectory, getRaioane } from "@/lib/api/agencies";
import {
  AGENCY_DIRECTORY_PAGE_SIZE,
  agencyDirectoryApiQuery,
  agencyDirectoryHref,
  agencyPath,
  parseAgencyDirectoryParams,
} from "@/lib/agency/publicPage";
import { siteUrl } from "@/lib/site";

type PageProps = { searchParams: Promise<Record<string, string | string[] | undefined>> };

export async function generateMetadata({ searchParams }: PageProps): Promise<Metadata> {
  const state = parseAgencyDirectoryParams(await searchParams);
  const t = await getTranslations("Agencies");
  const filtered = state.q !== undefined || state.raionId !== undefined || state.verified || state.page > 1;
  return {
    title: t("directoryMetaTitle"),
    description: t("directoryMetaDescription"),
    alternates: { canonical: `${siteUrl()}/agencies` },
    // A filtered or later page is the same directory — only the plain one is indexed.
    robots: filtered ? { index: false, follow: true } : undefined,
  };
}

// The public agency directory: search by name, filter by city and "verified only", verified
// agencies first. A plain GET form, so every filtered URL is a page of its own (and works without JS).
export default async function AgenciesPage({ searchParams }: PageProps) {
  const state = parseAgencyDirectoryParams(await searchParams);
  const [t, tSearch, locale, directory, raioane] = await Promise.all([
    getTranslations("Agencies"),
    getTranslations("Search"),
    getLocale(),
    getAgencyDirectory(agencyDirectoryApiQuery(state)),
    getRaioane(),
  ]);
  const total = directory?.totalCount ?? 0;
  const totalPages = Math.ceil(total / AGENCY_DIRECTORY_PAGE_SIZE);
  const filtered = state.q !== undefined || state.raionId !== undefined || state.verified;

  return (
    <div className="flex min-h-screen flex-col">
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 pb-12 sm:px-6">
        <h1 className="pt-8 font-hero text-2xl font-extrabold text-ink-950 sm:text-3xl">{t("directoryTitle")}</h1>
        <p className="mt-2 max-w-2xl text-sm text-ink-600">{t("directoryIntro")}</p>

        <AutoSubmitForm
          action="/agencies"
          className="mt-6 grid gap-3 rounded-2xl border border-ink-100 bg-white p-4 shadow-[var(--shadow-card)] sm:grid-cols-[minmax(0,1fr)_14rem_auto_auto] sm:items-end"
        >
          <label className="block">
            <span className="mb-1.5 block text-sm font-medium text-ink-800">{t("searchLabel")}</span>
            <TextInput type="search" name="q" defaultValue={state.q ?? ""} placeholder={t("searchPlaceholder")} maxLength={100} />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-sm font-medium text-ink-800">{t("cityLabel")}</span>
            <SelectInput name="raionId" defaultValue={state.raionId ?? ""}>
              <option value="">{t("allCities")}</option>
              {raioane.map((raion) => (
                <option key={raion.id} value={raion.id}>
                  {locale === "ru" && raion.nameRu ? raion.nameRu : raion.nameRo}
                </option>
              ))}
            </SelectInput>
          </label>
          <label className="flex h-11 items-center gap-2 text-sm text-ink-800">
            <input type="checkbox" name="verified" value="1" defaultChecked={state.verified} className="h-4 w-4 accent-brand-700" />
            {t("verifiedOnly")}
          </label>
          <Button type="submit" className="w-full sm:w-auto">
            {t("search")}
          </Button>
        </AutoSubmitForm>

        <div className="mt-6 flex flex-wrap items-baseline justify-between gap-2">
          <p className="text-sm text-ink-600">{directory === null ? t("directoryError") : t("agencyCount", { count: total })}</p>
          {filtered && (
            <Link href="/agencies" className="text-sm font-medium text-accent-700 hover:underline">
              {t("resetFilters")}
            </Link>
          )}
        </div>

        {directory && directory.items.length > 0 ? (
          <>
            <ul className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {directory.items.map((agency) => (
                <li key={agency.id}>
                  <Link
                    href={agencyPath(agency.slug)}
                    className="flex h-full items-center gap-4 rounded-2xl border border-ink-100 bg-white p-4 shadow-[var(--shadow-card)] transition-colors hover:border-ink-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600"
                  >
                    {agency.logoThumbnailUrl ? (
                      // eslint-disable-next-line @next/next/no-img-element -- blob storage URL
                      <img
                        src={agency.logoThumbnailUrl}
                        alt={t("logoAlt", { name: agency.name })}
                        width={64}
                        height={64}
                        className="h-16 w-16 shrink-0 rounded-xl border border-ink-100 object-cover"
                      />
                    ) : (
                      <Avatar userId={agency.id} displayName={agency.name} size={64} shape="square" className="shrink-0" />
                    )}
                    <span className="min-w-0">
                      <span className="flex items-center gap-1.5">
                        <span className="truncate font-semibold text-ink-950">{agency.name}</span>
                        {agency.isVerified && <VerifiedBadge />}
                      </span>
                      {agency.raionName && <span className="block truncate text-sm text-ink-500">{agency.raionName}</span>}
                      <span className="mt-0.5 block text-sm text-ink-700">{t("activeListings", { count: agency.activeListingCount })}</span>
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
            <LinkPagination
              page={state.page}
              totalPages={totalPages}
              hrefFor={(page) => agencyDirectoryHref({ ...state, page })}
              labels={{ nav: tSearch("pagination"), previous: tSearch("previous"), next: tSearch("next") }}
            />
          </>
        ) : (
          directory && (
            <div className="mt-4 flex flex-col items-center gap-2 rounded-[18px] border border-dashed border-line bg-white px-6 py-16 text-center">
              <p className="font-semibold text-ink-900">{t("directoryEmpty")}</p>
              {filtered && <p className="max-w-md text-sm text-ink-500">{t("directoryEmptyHint")}</p>}
            </div>
          )
        )}
      </main>
      <Footer />
    </div>
  );
}
