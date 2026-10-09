"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils/cn";
import type { UserProfile } from "@/lib/auth/profile";
import type { AccountDataSummary } from "@/lib/account/deletion";
import { ProfileForm } from "@/components/account/ProfileForm";
import { PasswordForm } from "@/components/account/PasswordForm";
import { OtherSessionsPanel } from "@/components/account/OtherSessionsPanel";
import { PrivacyPanel, type ExportStatus } from "@/components/account/PrivacyPanel";
import { NotificationsPanel } from "@/components/account/NotificationsPanel";
import type { EmailPreferences } from "@/lib/account/emailPreferences";
import { AgenciesPanel } from "@/components/account/AgenciesPanel";
import type { Invitation, MyAgency } from "@/types/agency";

export type AccountTab = "profile" | "agencies" | "security" | "notifications" | "privacy";

const tabIcon = {
  profile: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" className="h-4 w-4">
      <circle cx="12" cy="8.5" r="3.4" />
      <path d="M5 20c1.2-4 4-6 7-6s5.8 2 7 6" strokeLinecap="round" />
    </svg>
  ),
  agencies: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" className="h-4 w-4">
      <path d="M4.5 20.5V6.5l7-3v17M11.5 9.5l8 2.5v8.5" strokeLinejoin="round" />
      <path d="M3 20.5h18M7.5 9v.01M7.5 12.5v.01M7.5 16v.01M15 14.5v.01M15 17.5v.01" strokeLinecap="round" />
    </svg>
  ),
  security: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" className="h-4 w-4">
      <rect x="5" y="10.5" width="14" height="9.5" rx="2" />
      <path d="M8 10.5V7.5a4 4 0 0 1 8 0v3" />
    </svg>
  ),
  notifications: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" className="h-4 w-4">
      <path d="M6 16.5V11a6 6 0 1 1 12 0v5.5l1.5 2h-15l1.5-2Z" strokeLinejoin="round" />
      <path d="M10 20.5a2 2 0 0 0 4 0" strokeLinecap="round" />
    </svg>
  ),
  privacy: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" className="h-4 w-4">
      <path d="M12 3.5 5 6.5v5c0 4.3 3 7.8 7 9 4-1.2 7-4.7 7-9v-5l-7-3Z" strokeLinejoin="round" />
      <path d="m9.2 12 2 2 3.8-4" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  ),
};

export function AccountSettings({
  profile,
  summary = null,
  emailPreferences = null,
  agencies = null,
  invitations = null,
  initialTab = "profile",
  exportStatus,
}: {
  profile: UserProfile;
  summary?: AccountDataSummary | null;
  emailPreferences?: EmailPreferences | null;
  agencies?: MyAgency[] | null;
  invitations?: Invitation[] | null;
  initialTab?: AccountTab;
  exportStatus?: ExportStatus;
}) {
  const t = useTranslations("Account");
  const [tab, setTab] = useState<AccountTab>(initialTab);

  // The tab is in the address too (?tab=), so a reload or a shared link opens the same one.
  function select(next: AccountTab) {
    setTab(next);
    try {
      const url = new URL(window.location.href);
      if (next === "profile") url.searchParams.delete("tab");
      else url.searchParams.set("tab", next);
      url.searchParams.delete("export");
      window.history.replaceState(window.history.state, "", url);
    } catch {
      // The address isn't essential.
    }
  }

  const tabs: { id: AccountTab; label: string; badge?: number }[] = [
    { id: "profile", label: t("profileTab") },
    { id: "agencies", label: t("agenciesTab"), badge: invitations?.length || undefined },
    { id: "security", label: t("securityTab") },
    { id: "notifications", label: t("notificationsTab") },
    { id: "privacy", label: t("privacyTab") },
  ];

  return (
    <>
      <h1 className="font-display text-2xl font-medium text-ink-950 sm:text-[28px]">{t("settingsTitle")}</h1>
      <p className="mt-1.5 text-sm text-ink-500">{t("settingsSubtitle")}</p>

      <div className="mt-7 flex flex-col gap-6 md:flex-row md:items-start">
        {/* Five tabs: a two-column grid on phones (no hidden tab behind a sideways scroll; the last
            one takes the whole row), a wrapping row on tablets, then a column. */}
        <nav className="grid w-full grid-cols-2 gap-1 sm:flex sm:flex-wrap md:w-56 md:flex-shrink-0 md:flex-col md:flex-nowrap">
          {tabs.map((item) => (
            <button
              key={item.id}
              type="button"
              onClick={() => select(item.id)}
              aria-current={tab === item.id ? "page" : undefined}
              className={cn(
                "flex items-center gap-2.5 whitespace-nowrap rounded-xl px-3.5 py-2.5 text-left text-sm font-medium transition-colors last:col-span-2",
                tab === item.id ? "bg-brand-100/70 text-brand-700" : "text-ink-500 hover:bg-ink-50 hover:text-ink-900",
              )}
            >
              {tabIcon[item.id]}
              {item.label}
              {item.badge && (
                <span className="ml-auto inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-accent-500 px-1.5 text-xs font-semibold text-white">
                  {item.badge}
                  <span className="sr-only"> {t("pendingInvitations")}</span>
                </span>
              )}
            </button>
          ))}
        </nav>

        <div className="min-w-0 flex-1 rounded-2xl border border-ink-100 bg-white p-6 shadow-[var(--shadow-card)] sm:p-8">
          {tab === "profile" ? (
            <ProfileForm profile={profile} />
          ) : tab === "agencies" ? (
            <AgenciesPanel agencies={agencies} invitations={invitations} emailConfirmed={profile.emailConfirmed} />
          ) : tab === "security" ? (
            <>
              <PasswordForm hasPassword={profile.hasPassword} />
              <OtherSessionsPanel />
            </>
          ) : tab === "notifications" ? (
            <NotificationsPanel initial={emailPreferences} />
          ) : (
            <PrivacyPanel
              email={profile.email}
              hasPassword={profile.hasPassword}
              summary={summary}
              exportStatus={exportStatus}
            />
          )}
        </div>
      </div>
    </>
  );
}
