"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { siTelegram, siViber, siWhatsapp, type SimpleIcon } from "simple-icons";
import { revealAgencyPhone, revealListingPhone } from "@/lib/listing/contactActions";
import {
  formatPhone,
  maskedPhoneGlyphs,
  messagingAppHref,
  opensInNewTab,
  revealedPhoneGlyphs,
  type MessagingApp,
} from "@/lib/listing/contactCard";
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

type Problem = "unavailable" | "tooMany" | "failed";

// The call button and the messaging-app buttons of a listing's contact card. The page only has the
// number's shape (its first digits, how many follow); the full number comes from the API when the
// visitor asks for it — tapping the number or an app — so it isn't in the page for scrapers. The
// owner gets their own number as is.
//
// The reveal should feel like one thing changing, not the button being swapped: it stays the same
// <button> (focus stays put), the number sits on the left with every digit in a fixed-width slot (the
// masked and the real number share one format — see maskedPhoneGlyphs), only the hidden digits fade
// in, one after another, and the "Arată" pill fades out where it is without moving anything.
export function ContactActions({
  phoneOf,
  phonePrefix,
  hiddenDigits,
  ownPhone,
  apps,
  callHours,
}: {
  // Whose number it is: a listing's contact, or an agency (its own page).
  phoneOf: { listingId: string } | { agencyId: string };
  phonePrefix: string;
  hiddenDigits: number;
  ownPhone: string | null;
  apps: MessagingApp[];
  // "Sună între 09:00 și 19:00" — shown under the number, the button it's about.
  callHours: string | null;
}) {
  const t = useTranslations("PropertyDetail");
  const [phone, setPhone] = useState<string | null>(ownPhone);
  const [justRevealed, setJustRevealed] = useState(false);
  const [pending, setPending] = useState(false);
  const [problem, setProblem] = useState<Problem | null>(null);

  async function reveal(): Promise<string | null> {
    if (phone) return phone;
    if (pending) return null;
    setPending(true);
    setProblem(null);
    const result = "listingId" in phoneOf ? await revealListingPhone(phoneOf.listingId) : await revealAgencyPhone(phoneOf.agencyId);
    setPending(false);
    if (result.phone === null) {
      setProblem(result.reason);
      return null;
    }
    setPhone(result.phone);
    setJustRevealed(true);
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

  const glyphs = phone ? revealedPhoneGlyphs(phone, hiddenDigits) : maskedPhoneGlyphs(phonePrefix, hiddenDigits);
  let hiddenIndex = 0;
  const number = (
    <span className="whitespace-nowrap font-semibold tabular-nums" aria-hidden>
      {glyphs.map((glyph, i) => {
        const digitSlot = /[\d•]/.test(glyph.char);
        const animate = phone !== null && justRevealed && glyph.hidden;
        const delay = glyph.hidden ? hiddenIndex++ * 45 : 0;
        return (
          <span
            key={i}
            className={cn(digitSlot && "inline-block w-[1ch] text-center", animate && "motion-safe:animate-[digit-in_260ms_ease-out_both]")}
            style={animate ? { animationDelay: `${delay}ms` } : undefined}
          >
            {glyph.char}
          </span>
        );
      })}
    </span>
  );

  // One app gets a full-width "Scrie pe …" button; two or three share a row.
  const single = apps.length === 1;
  const appClass = cn(
    "flex h-11 min-w-0 items-center justify-center gap-2 rounded-full border border-ink-100 bg-white px-3 text-sm font-medium text-ink-900 transition-colors hover:border-ink-200 hover:bg-ink-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600 focus-visible:ring-offset-2 disabled:opacity-60",
    single && "w-full",
  );

  return (
    <div className="mt-4 space-y-2.5">
      <button
        type="button"
        onClick={() => {
          if (phone) window.location.href = `tel:${phone.replace(/[^\d+]/g, "")}`;
          else void reveal();
        }}
        aria-busy={pending}
        aria-label={phone ? t("callNumber", { phone: formatPhone(phone) }) : t("showPhoneLabel")}
        className="relative flex h-12 w-full items-center gap-3 rounded-full bg-ink-950 pl-5 pr-24 text-left text-base text-white shadow-sm transition-colors hover:bg-ink-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600 focus-visible:ring-offset-2"
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-[18px] w-[18px] shrink-0" aria-hidden>
          <path d="M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2Z" strokeLinejoin="round" />
        </svg>
        {number}
        {/* Fades out on reveal, where it is — nothing beside it moves. */}
        <span
          aria-hidden
          className={cn(
            "absolute right-2 top-1/2 flex h-8 w-[4.5rem] -translate-y-1/2 items-center justify-center rounded-full bg-white/15 text-xs font-medium transition-opacity duration-300",
            phone ? "opacity-0" : "opacity-100",
          )}
        >
          {pending ? (
            <svg viewBox="0 0 24 24" fill="none" className="h-4 w-4 animate-spin" aria-hidden>
              <circle cx="12" cy="12" r="9" stroke="currentColor" strokeOpacity="0.3" strokeWidth="3" />
              <path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
            </svg>
          ) : (
            t("showPhone")
          )}
        </span>
      </button>
      {/* For screen readers: the number, once it has arrived. */}
      <span className="sr-only" aria-live="polite">
        {phone && justRevealed ? formatPhone(phone) : ""}
      </span>
      {callHours && (
        <p className="flex items-center justify-center gap-1.5 text-sm text-ink-600">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" className="h-4 w-4 shrink-0 text-ink-400" aria-hidden>
            <circle cx="12" cy="12" r="9" />
            <path d="M12 7v5l3 2" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
          {callHours}
        </p>
      )}
      {problem && (
        <p role="alert" className="px-1 text-xs text-accent-700">
          {t(problem === "tooMany" ? "phoneRevealTooMany" : problem === "unavailable" ? "phoneUnavailable" : "phoneRevealFailed")}
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
