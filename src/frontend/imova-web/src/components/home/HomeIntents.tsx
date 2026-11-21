import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { searchHref } from "@/lib/search/filters";

// Right under the hero: the three things people come to do — buy, sell or rent out, rent — each a
// card with one button, so the hero itself can stay a single question. Selling gets the middle
// card: owners are the listings everyone else comes for.
const INTENTS = [
  {
    key: "buy",
    href: searchHref({ transactionType: ["Sale"] }),
    tint: "bg-brand-100 text-brand-700",
    icon: "M8 22 24 9l16 13M12 19v20h24V19M20 39V28h8v11",
  },
  {
    key: "sell",
    href: "/properties/new",
    tint: "bg-accent-100 text-accent-600",
    icon: "M10 40V18l14-9 14 9v22M10 40h28M19 40V26h10v14M31 7l5 4-5 4",
  },
  {
    key: "rent",
    href: searchHref({ transactionType: ["Rent"] }),
    tint: "bg-[#e3efe6] text-[#2f7a4a]",
    icon: "M11 42V8h26v34M7 42h34M17 15h4M27 15h4M17 23h4M27 23h4M21 42v-9h6v9",
  },
] as const;

export async function HomeIntents() {
  const t = await getTranslations("Home.intents");

  return (
    <section aria-label={t("label")} className="mx-auto max-w-6xl px-4 pt-8 sm:px-6 sm:pt-12">
      <ul className="grid gap-3 sm:gap-5 lg:grid-cols-3">
        {INTENTS.map((intent) => (
          <li key={intent.key}>
            <Link
              href={intent.href}
              className="group flex h-full items-center gap-4 rounded-2xl border border-ink-100 bg-white p-4 shadow-[var(--shadow-card)] transition-shadow hover:shadow-[var(--shadow-card-hover)] lg:flex-col lg:p-7 lg:text-center"
            >
              <span className={`flex h-16 w-16 shrink-0 items-center justify-center rounded-full lg:h-24 lg:w-24 ${intent.tint}`}>
                <svg viewBox="0 0 48 48" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" className="h-8 w-8 lg:h-12 lg:w-12" aria-hidden>
                  <path d={intent.icon} />
                </svg>
              </span>
              <span className="flex min-w-0 flex-1 flex-col gap-1 lg:items-center lg:gap-2">
                <span className="font-hero text-lg font-bold text-ink-950 lg:text-xl">{t(`${intent.key}.title`)}</span>
                <span className="text-sm text-ink-500 lg:max-w-[30ch]">{t(`${intent.key}.text`)}</span>
                <span className="mt-1 hidden rounded-lg border-[1.5px] border-accent-500 px-4 py-2 text-sm font-semibold text-accent-600 transition-colors group-hover:bg-accent-500 group-hover:text-white lg:mt-3 lg:inline-block">
                  {t(`${intent.key}.cta`)}
                </span>
              </span>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="h-5 w-5 shrink-0 text-ink-300 lg:hidden" aria-hidden>
                <path d="m9 18 6-6-6-6" />
              </svg>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
