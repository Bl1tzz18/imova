"use client";

import { useEffect, useRef, useState } from "react";
import { Link } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { Avatar } from "@/components/ui/Avatar";
import { logout } from "@/lib/auth/actions";
import type { UserProfile } from "@/lib/auth/profile";
import { badgeText } from "@/lib/admin/adminSections";
import { popoverItem, popoverPanel } from "@/components/ui/popover";

// The avatar's menu: your own pages (with icons), then — for admins — one "Administrare" entry
// with how many things wait there, then sign out.
export function AccountMenu({ profile, adminWaiting = 0 }: { profile: UserProfile; adminWaiting?: number }) {
  const t = useTranslations("Header");
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;

    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    function handleEscape(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }

    document.addEventListener("mousedown", handleClickOutside);
    document.addEventListener("keydown", handleEscape);

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("keydown", handleEscape);
    };
  }, [open]);

  const isAdmin = profile.roles.includes("Admin");
  const close = () => setOpen(false);

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-label={isAdmin && adminWaiting > 0 ? `${t("account")} · ${t("adminWaiting", { count: adminWaiting })}` : t("account")}
        aria-haspopup="menu"
        aria-expanded={open}
        className="relative rounded-full transition-shadow hover:ring-2 hover:ring-ink-200"
      >
        <Avatar
          userId={profile.id}
          displayName={profile.displayName}
          email={profile.email}
          pictureUrl={profile.profilePictureUrl}
          size={36}
        />
        {/* Admins: a dot on the avatar when something waits in the admin area. */}
        {isAdmin && adminWaiting > 0 && (
          <span aria-hidden className="absolute -right-0.5 -top-0.5 h-3 w-3 rounded-full border-2 border-white bg-accent-500" />
        )}
      </button>

      {/* On a phone the avatar sits too far left for a menu anchored to it, so the menu is a
          full-width panel under the header (fixed — the header's backdrop blur makes it the
          containing block); from sm up it hangs from the avatar. */}
      {open && (
        <div
          role="menu"
          className={`fixed inset-x-4 top-[calc(100%+8px)] z-30 origin-top-right overflow-hidden p-1.5 sm:absolute sm:inset-x-auto sm:right-0 sm:w-64 ${popoverPanel}`}
        >
          <AccountMenuItems profile={profile} adminWaiting={adminWaiting} onPick={close} />
        </div>
      )}
    </div>
  );
}

const menuItemClass = popoverItem;

// The menu's content — who you are, your pages, the admin area, sign out. Shared by the avatar's
// dropdown (desktop) and the phone tab bar's "Cont" sheet.
export function AccountMenuItems({ profile, adminWaiting, onPick }: { profile: UserProfile; adminWaiting: number; onPick: () => void }) {
  const t = useTranslations("Header");
  const isAdmin = profile.roles.includes("Admin");
  return (
    <>
      <div className="mb-1 border-b border-ink-100 px-3 pb-2.5 pt-2">
        <p className="truncate text-sm font-medium text-ink-900">{profile.displayName ?? profile.email}</p>
        <p className="truncate text-xs text-ink-400">{profile.email}</p>
      </div>

      {/* Your own things. Messages aren't here: they're the envelope beside the avatar (the Mesaje tab on a phone). */}
      <div className="py-1">
        {ACCOUNT_LINKS.map((link) => (
          <Link key={link.href} href={link.href} role="menuitem" onClick={onPick} className={menuItemClass}>
            <MenuIcon d={link.icon} />
            {t(link.label)}
          </Link>
        ))}
      </div>

      {/* The admin area is one entry; its sections are tabs there (AdminNav). */}
      {isAdmin && (
        <div className="border-t border-ink-100 py-1">
          <Link href="/admin" role="menuitem" onClick={onPick} className={`${menuItemClass} font-semibold text-ink-900`}>
            <MenuIcon d={ICONS.shield} className="text-accent-600" />
            <span className="flex-1">{t("administration")}</span>
            {adminWaiting > 0 && (
              <span className="rounded-full bg-accent-500 px-1.5 text-[11px] font-bold leading-[18px] text-white">
                <span aria-hidden>{badgeText(adminWaiting)}</span>
                <span className="sr-only">{t("adminWaiting", { count: adminWaiting })}</span>
              </span>
            )}
          </Link>
        </div>
      )}

      <form action={logout} className="border-t border-ink-100 pt-1">
        <button type="submit" role="menuitem" className={`${menuItemClass} text-ink-500`}>
          <MenuIcon d={ICONS.logout} />
          {t("logout")}
        </button>
      </form>
    </>
  );
}

const ICONS = {
  listings: "M4 6h16M4 12h16M4 18h10",
  heart: "M12 20s-7-4.4-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 10c0 5.6-7 10-7 10Z",
  bell: "M6 16v-5a6 6 0 1 1 12 0v5l1.5 2h-15ZM10 20a2 2 0 0 0 4 0",
  // A person in a circle: "your account" (a gear at this size reads as a sun).
  account: "M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18ZM12 12.5a3 3 0 1 0 0-6 3 3 0 0 0 0 6ZM6.5 18.2a6 6 0 0 1 11 0",
  shield: "M12 3 5 6v5c0 4.5 3 8.3 7 10 4-1.7 7-5.5 7-10V6Z",
  logout: "M15 4h3a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-3M10 16l-4-4 4-4M6 12h10",
} as const;

const ACCOUNT_LINKS = [
  { href: "/my-listings", label: "myListings", icon: ICONS.listings },
  { href: "/saved-listings", label: "savedListings", icon: ICONS.heart },
  { href: "/saved-searches", label: "savedSearches", icon: ICONS.bell },
  { href: "/account", label: "accountSettings", icon: ICONS.account },
] as const;

function MenuIcon({ d, className = "text-ink-400" }: { d: string; className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" className={`h-[18px] w-[18px] shrink-0 ${className}`} aria-hidden>
      <path d={d} />
    </svg>
  );
}
