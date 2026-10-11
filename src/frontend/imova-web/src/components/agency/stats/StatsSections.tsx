import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { Badge } from "@/components/ui/Badge";
import { LinkButton } from "@/components/ui/Button";
import { RenewListingButton } from "@/components/agency/stats/RenewListingButton";
import { attentionFix, change, contactRate, type Change } from "@/lib/agency/stats";
import { formatDate } from "@/lib/utils/format";
import { cn } from "@/lib/utils/cn";
import type { AgencyStats } from "@/types/agency";

const number = new Intl.NumberFormat("ru-RU");

// "↑ 12%" against the period before — green when it grew, red when it fell. Arrow + words, never
// colour alone.
async function ChangeChip({ value }: { value: Change }) {
  const t = await getTranslations("AgencyStats");
  const tone =
    value.kind === "up" || value.kind === "new"
      ? "bg-emerald-50 text-emerald-700"
      : value.kind === "down"
        ? "bg-red-50 text-red-700"
        : "bg-ink-100 text-ink-600";
  const text =
    value.kind === "up"
      ? `↑ ${value.percent}%`
      : value.kind === "down"
        ? `↓ ${value.percent}%`
        : value.kind === "new"
          ? t("changeNew")
          : t("changeSame");
  const label =
    value.kind === "up" || value.kind === "down" ? t(`change.${value.kind}`, { percent: value.percent }) : text;

  return (
    <span className={cn("inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium tabular-nums", tone)}>
      <span aria-hidden>{text}</span>
      <span className="sr-only">{label}</span>
    </span>
  );
}

// Contacts first and largest (what an agency is paying attention for), then views and saves. On a
// phone the contacts tile takes the whole row and the other two share the next.
export async function StatsKpis({ stats, showAgencyReveals }: { stats: AgencyStats; showAgencyReveals: boolean }) {
  const t = await getTranslations("AgencyStats");
  const { totals, previous } = stats;
  const rate = contactRate(totals.contacts, totals.views);
  const phoneReveals = totals.listingPhoneReveals + (showAgencyReveals ? totals.agencyPhoneReveals : 0);

  return (
    <div className="grid grid-cols-2 gap-3 lg:grid-cols-3">
      <section className="col-span-2 rounded-2xl border border-accent-300 bg-white p-4 sm:p-5 lg:col-span-1">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-ink-900">
          <span className="size-2.5 rounded-full bg-accent-500" aria-hidden />
          {t("contacts")}
        </h3>
        <p className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1">
          <span className="font-display text-4xl font-medium text-ink-950 tabular-nums">{number.format(totals.contacts)}</span>
          <ChangeChip value={change(totals.contacts, previous.contacts)} />
        </p>
        <p className="mt-2 text-sm text-ink-600">
          {t("contactsBreakdown", { phones: phoneReveals, messages: totals.messages })}
        </p>
        {showAgencyReveals && totals.agencyPhoneReveals > 0 && (
          <p className="mt-1 text-sm text-ink-500">{t("agencyReveals", { count: totals.agencyPhoneReveals })}</p>
        )}
        {rate !== null && <p className="mt-1 text-sm text-ink-500">{t("contactRate", { rate: rate.toLocaleString("ro-RO") })}</p>}
      </section>

      <section className="rounded-2xl border border-line bg-white p-4 sm:p-5">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-ink-900">
          <span className="size-2.5 rounded-full bg-brand-500" aria-hidden />
          {t("views")}
        </h3>
        <p className="mt-2 font-display text-3xl font-medium text-ink-950 tabular-nums">{number.format(totals.views)}</p>
        <div className="mt-2">
          <ChangeChip value={change(totals.views, previous.views)} />
        </div>
      </section>

      <section className="rounded-2xl border border-line bg-white p-4 sm:p-5">
        <h3 className="text-sm font-semibold text-ink-900">{t("favorites")}</h3>
        <p className="mt-2 font-display text-3xl font-medium text-ink-950 tabular-nums">{number.format(totals.favorites)}</p>
        <div className="mt-2">
          <ChangeChip value={change(totals.favorites, previous.favorites)} />
        </div>
      </section>
    </div>
  );
}

function Thumb({ url }: { url: string | null }) {
  return url ? (
    // eslint-disable-next-line @next/next/no-img-element
    <img src={url} alt="" className="size-12 shrink-0 rounded-lg object-cover" loading="lazy" />
  ) : (
    <span className="size-12 shrink-0 rounded-lg bg-ink-100" aria-hidden />
  );
}

// The listings that brought the most contacts (then views): a table on wider screens, rows of
// cards on a phone.
export async function TopListings({ stats }: { stats: AgencyStats }) {
  const t = await getTranslations("AgencyStats");
  if (stats.topListings.length === 0) return null;

  return (
    <section aria-labelledby="top-listings-title" className="rounded-2xl border border-line bg-white p-4 sm:p-5">
      <h2 id="top-listings-title" className="text-base font-semibold text-ink-950">
        {t("topTitle")}
      </h2>
      <p className="mt-1 text-sm text-ink-500">{t("topIntro")}</p>

      {/* Phone: one card per listing. */}
      <ol className="mt-4 divide-y divide-line sm:hidden">
        {stats.topListings.map((l) => (
          <li key={l.listingId} className="flex gap-3 py-3">
            <Thumb url={l.thumbnailUrl} />
            <div className="min-w-0 flex-1">
              <Link href={`/property/${l.listingId}`} className="line-clamp-2 text-sm font-medium text-ink-900 hover:underline">
                {l.title}
              </Link>
              <p className="mt-0.5 flex flex-wrap items-center gap-2 text-xs text-ink-500">
                <span>ID {l.number}</span>
                {l.status !== "Active" && <Badge>{t(`status.${l.status}`)}</Badge>}
              </p>
              <p className="mt-1 text-sm text-ink-700 tabular-nums">
                {t("rowSummary", { contacts: l.phoneReveals + l.messages, views: l.views, favorites: l.favorites })}
              </p>
            </div>
          </li>
        ))}
      </ol>

      {/* Wider: a table. */}
      <div className="mt-4 hidden sm:block">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-line text-ink-500">
            <tr>
              <th scope="col" className="py-2 pr-3 font-medium">{t("colListing")}</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">{t("contacts")}</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">{t("views")}</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">{t("colRate")}</th>
              <th scope="col" className="py-2 pl-3 text-right font-medium">{t("favorites")}</th>
            </tr>
          </thead>
          <tbody>
            {stats.topListings.map((l) => {
              const contacts = l.phoneReveals + l.messages;
              const rate = contactRate(contacts, l.views);
              return (
                <tr key={l.listingId} className="border-b border-line last:border-0">
                  <td className="py-3 pr-3">
                    <div className="flex items-center gap-3">
                      <Thumb url={l.thumbnailUrl} />
                      <div className="min-w-0">
                        <Link href={`/property/${l.listingId}`} className="line-clamp-1 font-medium text-ink-900 hover:underline">
                          {l.title}
                        </Link>
                        <p className="mt-0.5 flex items-center gap-2 text-xs text-ink-500">
                          <span>ID {l.number}</span>
                          {l.status !== "Active" && <Badge>{t(`status.${l.status}`)}</Badge>}
                        </p>
                      </div>
                    </div>
                  </td>
                  <td className="px-3 py-3 text-right font-semibold text-ink-950 tabular-nums">
                    {contacts}
                    <span className="block text-xs font-normal text-ink-500">
                      {t("contactsSplit", { phones: l.phoneReveals, messages: l.messages })}
                    </span>
                  </td>
                  <td className="px-3 py-3 text-right text-ink-800 tabular-nums">{number.format(l.views)}</td>
                  <td className="px-3 py-3 text-right text-ink-800 tabular-nums">
                    {rate === null ? "—" : `${rate.toLocaleString("ro-RO")}%`}
                  </td>
                  <td className="py-3 pl-3 text-right text-ink-800 tabular-nums">{l.favorites}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </section>
  );
}

// "De îmbunătățit": each listing with what's wrong and the one action that fixes it.
export async function NeedsAttentionList({ stats, locale }: { stats: AgencyStats; locale: string }) {
  const t = await getTranslations("AgencyStats");
  if (stats.needsAttention.length === 0) return null;

  return (
    <section aria-labelledby="attention-title" className="rounded-2xl border border-line bg-white p-4 sm:p-5">
      <h2 id="attention-title" className="flex items-center gap-2 text-base font-semibold text-ink-950">
        {t("attentionTitle")}
        <span className="rounded-full bg-accent-100 px-2 py-0.5 text-xs font-medium text-accent-700 tabular-nums">
          {stats.needsAttention.length}
        </span>
      </h2>
      <p className="mt-1 text-sm text-ink-500">{t("attentionIntro")}</p>

      <ul className="mt-3 divide-y divide-line">
        {stats.needsAttention.map((item) => {
          const fix = attentionFix(item.reason, item.listingId);
          return (
            <li key={`${item.listingId}-${item.reason}`} className="flex flex-wrap items-center gap-x-4 gap-y-2 py-3">
              <div className="min-w-0 flex-1 basis-56">
                <Link href={`/property/${item.listingId}`} className="line-clamp-1 text-sm font-medium text-ink-900 hover:underline">
                  {item.title}
                </Link>
                <p className="mt-0.5 text-sm text-ink-600">
                  {item.reason === "ExpiringSoon" && item.expiresAt
                    ? t("reason.ExpiringSoon", { date: formatDate(item.expiresAt, locale) })
                    : item.reason === "NoContacts"
                      ? t("reason.NoContacts", { views: item.views })
                      : t("reason.FewPhotos")}
                </p>
              </div>
              {fix.kind === "renew" ? (
                <RenewListingButton listingId={item.listingId} />
              ) : (
                <LinkButton href={fix.href} variant="secondary" size="sm">
                  {item.reason === "FewPhotos" ? t("fix.addPhotos") : t("fix.edit")}
                </LinkButton>
              )}
            </li>
          );
        })}
      </ul>
    </section>
  );
}
