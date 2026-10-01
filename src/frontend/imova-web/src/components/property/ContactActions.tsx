"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { siTelegram, siViber, siWhatsapp, type SimpleIcon } from "simple-icons";
import { revealListingPhone } from "@/lib/listing/contactActions";
import { formatPhone, messagingAppHref, opensInNewTab, type MessagingApp } from "@/lib/listing/contactCard";
import { cn } from "@/lib/utils/cn";

const APP_ICONS: Record<MessagingApp, SimpleIcon> = { WhatsApp: siWhatsapp, Viber: siViber, Telegram: siTelegram };

function AppIcon({ app, className }: { app: MessagingApp; className?: string }) {
  const icon = APP_ICONS[app];
  return (
    <svg viewBox="0 0 24 24" fill={`#${icon.hex}`} className={className} aria-hidden>
      <path d={icon.path} />
    </svg>
  );
}

// The call button and the messaging-app buttons of a listing's contact card. The page only hands
// over the half-hidden number; the full one is fetched when the visitor asks for it (tapping the
// number or an app), so it isn't in the page for scrapers. The owner gets their own number as is.
export function ContactActions({
  listingId,
  maskedPhone,
  ownPhone,
  apps,
  callHours,
}: {
  listingId: string;
  maskedPhone: string;
  ownPhone: string | null;
  apps: MessagingApp[];
  // "Sună între 09:00 și 19:00" — shown under the number, the button it's about.
  callHours: string | null;
}) {
  const t = useTranslations("PropertyDetail");
  const [phone, setPhone] = useState<string | null>(ownPhone);
  const [pending, setPending] = useState(false);
  const [failed, setFailed] = useState(false);

  async function reveal(): Promise<string | null> {
    if (phone) return phone;
    setPending(true);
    setFailed(false);
    const result = await revealListingPhone(listingId);
    setPending(false);
    setPhone(result.phone);
    setFailed(result.phone === null);
    return result.phone;
  }

  // An app tapped before the number is shown: fetch it, then open the chat. A web link's tab is
  // opened right away (still inside the tap, so it isn't blocked as a pop-up) and pointed at the
  // chat once the number is here.
  async function openApp(app: MessagingApp) {
    const tab = opensInNewTab(app) ? window.open("about:blank", "_blank") : null;
    const number = await reveal();
    if (!number) {
      tab?.close();
      return;
    }
    const href = messagingAppHref(app, number);
    if (tab) {
      tab.opener = null;
      tab.location.href = href;
    } else {
      window.location.href = href;
    }
  }

  const phoneClass =
    "flex h-12 w-full items-center justify-center gap-2.5 rounded-full bg-ink-950 px-5 text-base font-semibold text-white shadow-sm transition-colors hover:bg-ink-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600 focus-visible:ring-offset-2";
  const phoneIcon = (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-[18px] w-[18px] shrink-0" aria-hidden>
      <path d="M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2Z" strokeLinejoin="round" />
    </svg>
  );

  // One app gets a full-width "Scrie pe …" button; two or three share a row.
  const single = apps.length === 1;
  const appClass = cn(
    "flex h-11 min-w-0 items-center justify-center gap-2 rounded-full border border-ink-100 bg-white px-3 text-sm font-medium text-ink-900 transition-colors hover:border-ink-200 hover:bg-ink-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600 focus-visible:ring-offset-2 disabled:opacity-60",
    single && "w-full",
  );

  return (
    <div className="mt-4 space-y-2.5">
      {phone ? (
        <a href={`tel:${phone.replace(/[^\d+]/g, "")}`} className={phoneClass} aria-label={t("callNumber", { phone: formatPhone(phone) })}>
          {phoneIcon}
          <span className="tabular-nums" aria-live="polite">
            {formatPhone(phone)}
          </span>
        </a>
      ) : (
        <button
          type="button"
          onClick={() => void reveal()}
          disabled={pending}
          aria-label={t("showPhoneLabel")}
          className={cn(phoneClass, "disabled:opacity-80")}
        >
          {phoneIcon}
          <span className="tabular-nums">{maskedPhone}</span>
          <span className="ml-1 rounded-full bg-white/15 px-2.5 py-0.5 text-xs font-medium">
            {pending ? t("showingPhone") : t("showPhone")}
          </span>
        </button>
      )}
      {callHours && (
        <p className="flex items-center justify-center gap-1.5 text-sm text-ink-600">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" className="h-4 w-4 shrink-0 text-ink-400" aria-hidden>
            <circle cx="12" cy="12" r="9" />
            <path d="M12 7v5l3 2" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
          {callHours}
        </p>
      )}
      {failed && (
        <p role="alert" className="px-1 text-xs text-accent-700">
          {t("phoneRevealFailed")}
        </p>
      )}

      {apps.length > 0 && (
        <div className={cn("gap-2", single ? "flex" : "grid", apps.length === 2 && "grid-cols-2", apps.length === 3 && "grid-cols-3")}>
          {apps.map((app) => {
            const label = single ? t("writeOn", { app }) : app;
            const content = (
              <>
                <AppIcon app={app} className="h-[18px] w-[18px] shrink-0" />
                <span className="truncate">{label}</span>
              </>
            );
            return phone ? (
              <a
                key={app}
                href={messagingAppHref(app, phone)}
                target={opensInNewTab(app) ? "_blank" : undefined}
                rel={opensInNewTab(app) ? "noopener noreferrer" : undefined}
                aria-label={t("writeOn", { app })}
                className={appClass}
              >
                {content}
              </a>
            ) : (
              <button
                key={app}
                type="button"
                onClick={() => void openApp(app)}
                disabled={pending}
                aria-label={t("writeOn", { app })}
                className={appClass}
              >
                {content}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
