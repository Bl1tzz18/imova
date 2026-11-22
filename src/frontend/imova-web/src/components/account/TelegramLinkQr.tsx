"use client";

import { useMemo } from "react";
import { useTranslations } from "next-intl";
import { renderSVG } from "uqr";
import { cn } from "@/lib/utils/cn";

// The "Conectează Telegram" link as a QR code, for a computer: many people have Telegram only on
// their phone, so they scan it, Telegram opens there, they press Start — and this page notices
// (useTelegramConnect). Shown only with a precise pointer (mouse, trackpad): on a phone or tablet
// the user is already on the device, and the "Deschide Telegram" button is the way.
export function TelegramLinkQr({ url, className }: { url: string; className?: string }) {
  const t = useTranslations("Account");
  // Generated here from the link (uqr, no network); the SVG is ours, not user content.
  const svg = useMemo(() => renderSVG(url, { border: 1 }), [url]);

  return (
    <div className={cn("hidden items-center gap-3 [@media(pointer:fine)]:flex", className)}>
      <div
        role="img"
        aria-label={t("telegramQrLabel")}
        className="h-28 w-28 shrink-0 overflow-hidden rounded-lg border border-ink-100 bg-white p-1 [&>svg]:h-full [&>svg]:w-full"
        dangerouslySetInnerHTML={{ __html: svg }}
      />
      <div className="min-w-0 text-sm">
        <p className="font-medium text-ink-900">{t("telegramQrTitle")}</p>
        <p className="mt-1 text-ink-500">{t("telegramQrHint")}</p>
      </div>
    </div>
  );
}
