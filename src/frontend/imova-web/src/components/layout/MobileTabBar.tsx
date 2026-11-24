"use client";

import { useEffect, useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { Link, usePathname } from "@/i18n/navigation";
import { Avatar } from "@/components/ui/Avatar";
import { useRealtime } from "@/components/messaging/RealtimeProvider";
import { unreadBadgeLabel } from "@/lib/messaging/thread";
import { activeTab, showsTabBar, type Tab } from "@/lib/navigation/tabBar";
import type { UserProfile } from "@/lib/auth/profile";
import { cn } from "@/lib/utils/cn";
import { AccountMenuItems } from "./AccountMenu";

const ICONS = {
  home: "M4 10.5 12 4l8 6.5V20a1 1 0 0 1-1 1h-4.5v-6h-5v6H5a1 1 0 0 1-1-1v-9.5Z",
  messages: "M4 5h16v11H9l-5 4V5Z",
  saved: "M12 20s-7-4.4-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 10c0 5.6-7 10-7 10Z",
  account: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM4 21a8 8 0 0 1 16 0",
} as const;

// The phone's main places, at the bottom where the thumb is (below md; the desktop header has them
// instead): Acasă, Mesaje (live unread count), "+" Adaugă anunț, Salvate, Cont — the 999.md layout
// people already know. Signed out, Mesaje and Salvate go through sign-in (those pages redirect) and
// Cont is the sign-in page; signed in, Cont opens the account menu as a bottom sheet. The forms and a
// conversation keep the bottom for their own controls, so they don't show it; a listing's contact bar
// sits on top of it — lib/navigation/tabBar.ts.
export function MobileTabBar({ profile, adminWaiting }: { profile: UserProfile | null; adminWaiting: number }) {
  const t = useTranslations("TabBar");
  const pathname = usePathname();
  const { unreadCount } = useRealtime();
  const [accountOpen, setAccountOpen] = useState(false);

  // A new page closes the sheet (a link in it was followed).
  useEffect(() => setAccountOpen(false), [pathname]);

  if (!showsTabBar(pathname)) return null;
  const active = accountOpen ? "account" : activeTab(pathname);
  const unread = profile ? unreadBadgeLabel(unreadCount) : null;
  const isAdmin = profile?.roles.includes("Admin") ?? false;

  return (
    <>
      {/* Keeps the end of every page clear of the bar. The bar is z-30 like the header and comes later
          in the page, so its raised "+" draws over a listing's contact bar resting on it. */}
      <div aria-hidden className="h-[calc(4rem+env(safe-area-inset-bottom))] md:hidden" />

      <nav
        aria-label={t("label")}
        className="fixed inset-x-0 bottom-0 z-30 border-t border-ink-100 bg-white/95 pb-[env(safe-area-inset-bottom)] backdrop-blur md:hidden"
      >
        <ul className="mx-auto grid h-16 max-w-md grid-cols-5 items-end">
          <TabLink href="/" tab="home" active={active} label={t("home")} icon={<TabIcon d={ICONS.home} />} />
          <TabLink
            href="/messages"
            tab="messages"
            active={active}
            label={t("messages")}
            ariaLabel={unread ? `${t("messages")} · ${t("unread", { count: unreadCount })}` : undefined}
            icon={
              <span className="relative">
                <TabIcon d={ICONS.messages} />
                {unread && (
                  <span aria-hidden className="absolute -right-2.5 -top-1.5 flex h-[18px] min-w-[18px] items-center justify-center rounded-full border-2 border-white bg-accent-500 px-1 text-[10px] font-bold text-white">
                    {unread}
                  </span>
                )}
              </span>
            }
          />
          <li className="flex justify-center">
            <Link
              href="/properties/new"
              aria-label={t("addListing")}
              className="flex flex-col items-center gap-0.5 pb-2.5 text-[11px] font-medium text-ink-600 focus-visible:outline-none"
            >
              <span className="-mt-4 flex h-12 w-12 items-center justify-center rounded-full border-[3px] border-white bg-accent-500 text-white shadow-[0_6px_14px_-4px_rgba(232,106,51,0.7)] transition-colors active:bg-accent-600">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.6" strokeLinecap="round" className="h-6 w-6" aria-hidden>
                  <path d="M12 5v14M5 12h14" />
                </svg>
              </span>
              <span aria-hidden>{t("add")}</span>
            </Link>
          </li>
          <TabLink href="/saved-listings" tab="saved" active={active} label={t("saved")} icon={<TabIcon d={ICONS.saved} />} />
          {profile ? (
            <li>
              <button
                type="button"
                onClick={() => setAccountOpen((open) => !open)}
                aria-expanded={accountOpen}
                aria-haspopup="menu"
                className={tabClass(active === "account")}
              >
                <span className={cn("relative rounded-full", active === "account" && "ring-2 ring-accent-500 ring-offset-2")}>
                  <Avatar userId={profile.id} displayName={profile.displayName} email={profile.email} pictureUrl={profile.profilePictureUrl} size={24} />
                  {isAdmin && adminWaiting > 0 && (
                    <span aria-hidden className="absolute -right-1 -top-1 h-2.5 w-2.5 rounded-full border-2 border-white bg-accent-500" />
                  )}
                </span>
                {t("account")}
              </button>
            </li>
          ) : (
            <TabLink href="/login" tab="account" active={active} label={t("account")} icon={<TabIcon d={ICONS.account} />} />
          )}
        </ul>
      </nav>

      {profile && accountOpen && (
        <AccountSheet onClose={() => setAccountOpen(false)} label={t("account")}>
          <AccountMenuItems profile={profile} adminWaiting={adminWaiting} onPick={() => setAccountOpen(false)} />
        </AccountSheet>
      )}
    </>
  );
}

function tabClass(on: boolean) {
  return cn(
    "flex h-16 w-full flex-col items-center justify-center gap-1 text-[11px] font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-500",
    on ? "text-accent-600" : "text-ink-500 active:text-ink-900",
  );
}

function TabLink({
  href,
  tab,
  active,
  label,
  ariaLabel,
  icon,
}: {
  href: string;
  tab: Tab;
  active: Tab | null;
  label: string;
  ariaLabel?: string;
  icon: ReactNode;
}) {
  const on = active === tab;
  return (
    <li>
      <Link href={href} aria-label={ariaLabel} aria-current={on ? "page" : undefined} className={tabClass(on)}>
        {icon}
        {label}
      </Link>
    </li>
  );
}

function TabIcon({ d }: { d: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className="h-6 w-6" aria-hidden>
      <path d={d} />
    </svg>
  );
}

// The account menu as a sheet over the bar: tap outside or Escape closes it. The bottom padding
// keeps the last row clear of the raised "+" button.
function AccountSheet({ onClose, label, children }: { onClose: () => void; label: string; children: ReactNode }) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onClose();
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [onClose]);

  return (
    <div className="fixed inset-0 z-[19] md:hidden">
      <div aria-hidden className="absolute inset-0 bg-ink-950/30" onClick={onClose} />
      <div
        role="menu"
        aria-label={label}
        className="absolute inset-x-0 bottom-[calc(4rem+env(safe-area-inset-bottom))] max-h-[70vh] overflow-y-auto rounded-t-2xl border-t border-ink-100 bg-white pb-6 pt-1.5 shadow-[0_-12px_32px_-16px_rgba(11,22,32,0.35)]"
      >
        {children}
      </div>
    </div>
  );
}
