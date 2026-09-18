"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Avatar } from "@/components/ui/Avatar";
import { logout } from "@/lib/auth/actions";
import type { UserProfile } from "@/lib/auth/profile";

export function AccountMenu({ profile }: { profile: UserProfile }) {
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

  const menuItemClass = "block px-3.5 py-2.5 text-sm text-ink-700 transition-colors hover:bg-ink-50";

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-label={t("account")}
        aria-haspopup="menu"
        aria-expanded={open}
        className="rounded-full transition-shadow hover:ring-2 hover:ring-ink-200"
      >
        <Avatar
          userId={profile.id}
          displayName={profile.displayName}
          email={profile.email}
          pictureUrl={profile.profilePictureUrl}
          size={36}
        />
      </button>

      {open && (
        <div
          role="menu"
          className="absolute right-0 top-[calc(100%+8px)] z-30 w-56 overflow-hidden rounded-xl border border-ink-100 bg-white py-1.5 shadow-[var(--shadow-card)]"
        >
          <div className="border-b border-ink-100 px-3.5 py-2.5">
            <p className="truncate text-sm font-medium text-ink-900">{profile.displayName ?? profile.email}</p>
            <p className="truncate text-xs text-ink-400">{profile.email}</p>
          </div>

          <Link href="/saved-listings" role="menuitem" onClick={() => setOpen(false)} className={menuItemClass}>
            {t("savedListings")}
          </Link>
          <Link href="/account" role="menuitem" onClick={() => setOpen(false)} className={menuItemClass}>
            {t("accountSettings")}
          </Link>

          <form action={logout}>
            <button type="submit" role="menuitem" className={`w-full text-left ${menuItemClass}`}>
              {t("logout")}
            </button>
          </form>
        </div>
      )}
    </div>
  );
}
