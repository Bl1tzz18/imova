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

export type AccountTab = "profile" | "security" | "notifications" | "privacy";

const tabIcon = {
  profile: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" className="h-4 w-4">
      <circle cx="12" cy="8.5" r="3.4" />
      <path d="M5 20c1.2-4 4-6 7-6s5.8 2 7 6" strokeLinecap="round" />
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
  initialTab = "profile",
  exportStatus,
}: {
  profile: UserProfile;
  summary?: AccountDataSummary | null;
  emailPreferences?: EmailPreferences | null;
  initialTab?: AccountTab;
  exportStatus?: ExportStatus;
}) {
  const t = useTranslations("Account");
  const [tab, setTab] = useState<AccountTab>(initialTab);

  const tabs: { id: AccountTab; label: string }[] = [
    { id: "profile", label: t("profileTab") },
    { id: "security", label: t("securityTab") },
    { id: "notifications", label: t("notificationsTab") },
    { id: "privacy", label: t("privacyTab") },
  ];

  return (
    <>
      <h1 className="font-display text-2xl font-medium text-ink-950 sm:text-[28px]">{t("settingsTitle")}</h1>
      <p className="mt-1.5 text-sm text-ink-500">{t("settingsSubtitle")}</p>

      <div className="mt-7 flex flex-col gap-6 md:flex-row md:items-start">
        {/* Four tabs: a 2×2 grid on phones (no hidden tab behind a sideways scroll), a row, then a column. */}
        <nav className="grid w-full grid-cols-2 gap-1 sm:flex md:w-56 md:flex-shrink-0 md:flex-col">
          {tabs.map((item) => (
            <button
              key={item.id}
              type="button"
              onClick={() => setTab(item.id)}
              aria-current={tab === item.id ? "page" : undefined}
              className={cn(
                "flex items-center gap-2.5 whitespace-nowrap rounded-xl px-3.5 py-2.5 text-left text-sm font-medium transition-colors",
                tab === item.id ? "bg-brand-100/70 text-brand-700" : "text-ink-500 hover:bg-ink-50 hover:text-ink-900",
              )}
            >
              {tabIcon[item.id]}
              {item.label}
            </button>
          ))}
        </nav>

        <div className="min-w-0 flex-1 rounded-2xl border border-ink-100 bg-white p-6 shadow-[var(--shadow-card)] sm:p-8">
          {tab === "profile" ? (
            <ProfileForm profile={profile} />
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
