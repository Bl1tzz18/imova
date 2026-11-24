"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { Button } from "@/components/ui/Button";
import { readPublicConfig } from "@/lib/api/publicConfig";
import { OPEN_CONSENT_EVENT, readConsent, saveConsent } from "@/lib/consent/consent";

// Asks once whether analytics cookies may be used (lib/consent). Only where analytics exists (a
// PostHog key), so local and CI never see it. A modal native <dialog>: centred on a computer, a
// bottom sheet on a phone (buttons within the thumb's reach). Refusing is exactly as easy as
// accepting — same size, same look, side by side (GDPR / Law 195/2024: no "Accept" + "Settings"
// without a "Refuse"). Escape doesn't close it: an answer is needed, and the site works either way.
// The footer's "Setări cookie-uri" opens it again to change the answer.
export function ConsentBanner() {
  const t = useTranslations("Consent");
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    if (!readPublicConfig().posthogKey) return;
    if (readConsent() === null) setOpen(true);
    const reopen = () => setOpen(true);
    window.addEventListener(OPEN_CONSENT_EVENT, reopen);
    return () => window.removeEventListener(OPEN_CONSENT_EVENT, reopen);
  }, []);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!open || !dialog || dialog.open) return;
    dialog.showModal();
    // Focus starts on the title — not on a button (no nudge either way) or the policy link.
    dialog.querySelector<HTMLElement>("#consent-title")?.focus();
  }, [open]);

  if (!open) return null;

  function answer(analytics: boolean) {
    saveConsent({ analytics });
    dialogRef.current?.close();
    setOpen(false);
  }

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby="consent-title"
      onCancel={(e) => e.preventDefault()}
      className="fixed inset-x-0 bottom-0 top-auto m-0 max-h-[85dvh] w-full max-w-none overflow-y-auto rounded-t-3xl bg-white p-5 pb-[calc(1.25rem+env(safe-area-inset-bottom))] text-ink-950 shadow-2xl backdrop:bg-ink-950/50 md:inset-0 md:m-auto md:max-h-[90vh] md:max-w-lg md:rounded-3xl md:p-7"
    >
      <div aria-hidden className="mx-auto mb-4 h-1 w-10 rounded-full bg-ink-200 md:hidden" />
      <h2 id="consent-title" tabIndex={-1} className="font-display text-xl font-semibold outline-none">
        {t("title")}
      </h2>
      <div className="mt-3 space-y-2.5 text-sm leading-relaxed text-ink-600">
        <p>{t("necessary")}</p>
        <p>{t("statistics")}</p>
        <p>
          {t.rich("choice", {
            link: (chunks) => (
              <Link href="/privacy#cookies" className="font-medium text-brand-700 underline underline-offset-2 hover:text-brand-800">
                {chunks}
              </Link>
            ),
          })}
        </p>
      </div>
      <div className="mt-5 grid grid-cols-2 gap-3">
        <Button variant="primary" onClick={() => answer(false)}>
          {t("reject")}
        </Button>
        <Button variant="primary" onClick={() => answer(true)}>
          {t("accept")}
        </Button>
      </div>
    </dialog>
  );
}

// The footer's "Setări cookie-uri": opens the banner again. Nothing to set without analytics.
export function CookieSettingsButton({ className, children }: { className?: string; children: React.ReactNode }) {
  const [enabled, setEnabled] = useState(false);
  useEffect(() => setEnabled(Boolean(readPublicConfig().posthogKey)), []);
  if (!enabled) return null;
  return (
    <button type="button" className={className} onClick={() => window.dispatchEvent(new Event(OPEN_CONSENT_EVENT))}>
      {children}
    </button>
  );
}
