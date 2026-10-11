import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { LinkButton } from "@/components/ui/Button";
import { DailyBarChart } from "@/components/agency/stats/DailyBarChart";
import { NeedsAttentionList, StatsKpis, TopListings } from "@/components/agency/stats/StatsSections";
import { getAgencyForMember, getAgencyStats } from "@/lib/api/agencies";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { agencyManagePath, canManage } from "@/lib/agency/manage";
import { moldovaToday, parseStatsPreset, STATS_PRESETS, statsApiQuery } from "@/lib/agency/stats";
import { cn } from "@/lib/utils/cn";

export async function generateMetadata({ params }: { params: Promise<{ id: string }> }): Promise<Metadata> {
  const { id } = await params;
  const [agency, t] = await Promise.all([getAgencyForMember(id), getTranslations("AgencyStats")]);
  return { title: agency ? t("pageTitle", { name: agency.name }) : undefined, robots: { index: false } };
}

// "9 oct." for the period line — date-only days, so UTC.
function shortDay(day: string, locale: string): string {
  return new Intl.DateTimeFormat(locale, { day: "numeric", month: "short", timeZone: "UTC" }).format(new Date(`${day}T00:00:00Z`));
}

// The Statistici tab: the last 7/30/90 days (?days=), led by contacts (phone numbers asked for and
// conversations started), compared with the period just before; contacts and views per day; the
// listings to improve, each with the action that fixes it; and the ones that work best. An Agent
// sees only their own listings.
export default async function AgencyStatsPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const [{ id }, query] = await Promise.all([params, searchParams]);
  const days = parseStatsPreset(query);
  const [agency, profile, stats, t, locale] = await Promise.all([
    getAgencyForMember(id),
    getCurrentUserProfile(),
    getAgencyStats(id, statsApiQuery(days, moldovaToday(new Date()))),
    getTranslations("AgencyStats"),
    getLocale(),
  ]);
  if (!agency || !profile || !stats) notFound();

  const managesAll = canManage(agency.myRole, profile.roles.includes("Admin"));
  const base = agencyManagePath(agency.id, "stats");
  const empty =
    stats.totals.views === 0 && stats.totals.contacts === 0 && stats.totals.favorites === 0 && stats.topListings.length === 0;

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="min-w-0">
          <p className="text-sm text-ink-700">
            {t("period", { days, from: shortDay(stats.from, locale), to: shortDay(stats.to, locale) })}
          </p>
          <p className="text-sm text-ink-500">{managesAll ? t("intro") : t("introAgent")}</p>
        </div>
        <nav aria-label={t("periodLabel")} className="flex gap-1 rounded-full bg-ink-100/70 p-1">
          {STATS_PRESETS.map((preset) => (
            <Link
              key={preset}
              href={preset === 30 ? base : `${base}?days=${preset}`}
              aria-current={preset === days ? "page" : undefined}
              className={cn(
                "flex h-9 min-w-16 items-center justify-center rounded-full px-3 text-sm font-medium transition-colors",
                preset === days ? "bg-white text-ink-950 shadow-sm" : "text-ink-600 hover:text-ink-900",
              )}
            >
              {t("preset", { days: preset })}
            </Link>
          ))}
        </nav>
      </div>

      {empty ? (
        <section className="rounded-2xl border border-dashed border-ink-200 bg-white px-5 py-10 text-center">
          <h2 className="text-base font-semibold text-ink-950">{t("emptyTitle")}</h2>
          <p className="mx-auto mt-2 max-w-md text-sm text-ink-500">{t("emptyBody")}</p>
          {agency.status === "Active" && agency.myRole && (
            <LinkButton href={`/properties/new?agencyId=${encodeURIComponent(agency.id)}`} size="sm" className="mt-5">
              {t("addListing")}
            </LinkButton>
          )}
        </section>
      ) : (
        <>
          <StatsKpis stats={stats} showAgencyReveals={managesAll} />
          <div className="grid gap-3 lg:grid-cols-2">
            <DailyBarChart
              title={t("contactsPerDay")}
              points={stats.daily.map((d) => ({ day: d.day, value: d.contacts }))}
              locale={locale}
              barClassName="bg-accent-500"
            />
            <DailyBarChart
              title={t("viewsPerDay")}
              points={stats.daily.map((d) => ({ day: d.day, value: d.views }))}
              locale={locale}
              barClassName="bg-brand-500"
            />
          </div>
        </>
      )}

      <NeedsAttentionList stats={stats} locale={locale} />
      <TopListings stats={stats} />
    </div>
  );
}
