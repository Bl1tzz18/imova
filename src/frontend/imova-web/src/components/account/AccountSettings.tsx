"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils/cn";
import type { UserProfile } from "@/lib/auth/profile";
import { ProfileForm } from "@/components/account/ProfileForm";
import { PasswordForm } from "@/components/account/PasswordForm";

type Tab = "profile" | "security";

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
};

export function AccountSettings({ profile }: { profile: UserProfile }) {
  const t = useTranslations("Account");
  const [tab, setTab] = useState<Tab>("profile");

  const tabs: { id: Tab; label: string }[] = [
    { id: "profile", label: t("profileTab") },
    { id: "security", label: t("securityTab") },
  ];

  return (
    <>
      <h1 className="font-display text-2xl font-medium text-ink-950 sm:text-[28px]">{t("settingsTitle")}</h1>
      <p className="mt-1.5 text-sm text-ink-500">{t("settingsSubtitle")}</p>

      <div className="mt-7 flex flex-col gap-6 md:flex-row md:items-start">
        <nav className="flex w-full gap-1 overflow-x-auto md:w-56 md:flex-shrink-0 md:flex-col">
          {tabs.map((item) => (
            <button
              key={item.id}
              type="button"
              onClick={() => setTab(item.id)}
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
          {tab === "profile" ? <ProfileForm profile={profile} /> : <PasswordForm hasPassword={profile.hasPassword} />}
        </div>
      </div>
    </>
  );
}
