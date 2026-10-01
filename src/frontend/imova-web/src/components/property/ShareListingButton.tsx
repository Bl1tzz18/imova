"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { siFacebook, siTelegram, siViber, siWhatsapp, type SimpleIcon } from "simple-icons";
import { DropdownMenu } from "@/components/ui/DropdownMenu";
import { shareHref, type ShareTarget } from "@/lib/listing/share";

const TARGETS: { id: ShareTarget; label: string; icon: SimpleIcon }[] = [
  { id: "whatsapp", label: "WhatsApp", icon: siWhatsapp },
  { id: "viber", label: "Viber", icon: siViber },
  { id: "telegram", label: "Telegram", icon: siTelegram },
  { id: "facebook", label: "Facebook", icon: siFacebook },
];

function BrandIcon({ icon }: { icon: SimpleIcon }) {
  return (
    <svg viewBox="0 0 24 24" fill={`#${icon.hex}`} className="h-[18px] w-[18px] shrink-0" aria-hidden>
      <path d={icon.path} />
    </svg>
  );
}

// "Distribuie" — a round icon on the listing's main photo, beside the heart. On a phone or tablet
// the device's own share sheet opens, with every app and contact the person has; on a computer a
// menu: copy the link, WhatsApp, Viber, Telegram, Facebook. `url` is the listing's canonical address.
export function ShareListingButton({ url, title, text }: { url: string; title: string; text: string }) {
  const t = useTranslations("PropertyDetail");
  const [native, setNative] = useState(false);
  const [copied, setCopied] = useState(false);

  // Only known in the browser — the server renders the menu version first. The device's own sheet
  // only on a touch device (phone, tablet), where it lists the person's apps and contacts; a computer
  // has one too (Windows, macOS), but it lacks Viber, Telegram and "copy link", so it gets our menu.
  useEffect(() => {
    setNative(
      typeof navigator.share === "function" && window.matchMedia("(pointer: coarse) and (hover: none)").matches,
    );
  }, []);

  useEffect(() => {
    if (!copied) return;
    const timer = setTimeout(() => setCopied(false), 2000);
    return () => clearTimeout(timer);
  }, [copied]);

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
    } catch {
      // No clipboard access (an insecure context, a blocked permission): show the link to copy by hand.
      window.prompt(t("copyLinkPrompt"), url);
    }
  }

  const triggerClass =
    "flex h-10 w-10 items-center justify-center rounded-full bg-white/90 text-ink-700 shadow-sm backdrop-blur transition-colors hover:bg-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600";
  const content = copied ? (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-[18px] w-[18px] text-brand-700" aria-hidden>
      <path d="m5 12 5 5L20 7" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  ) : (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-[18px] w-[18px]" aria-hidden>
      <path d="M12 3v12M7 8l5-5 5 5M5 14v5a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );

  return (
    <>
      {native ? (
        <button
          type="button"
          aria-label={t("share")}
          title={t("share")}
          className={triggerClass}
          onClick={() => {
            // Closing the sheet without sharing rejects — nothing to report.
            navigator.share({ title, text, url }).catch(() => {});
          }}
        >
          {content}
        </button>
      ) : (
        <DropdownMenu
          label={t("share")}
          triggerClassName={triggerClass}
          trigger={content}
          items={[
            {
              id: "copy",
              label: t("copyLink"),
              onSelect: () => void copyLink(),
              icon: (
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-[18px] w-[18px] shrink-0 text-ink-500" aria-hidden>
                  <path d="M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7l-1 1M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7l1-1" strokeLinecap="round" />
                </svg>
              ),
            },
            ...TARGETS.map((target) => ({
              id: target.id,
              label: target.label,
              href: shareHref(target.id, url, text),
              external: target.id !== "viber",
              icon: <BrandIcon icon={target.icon} />,
            })),
          ]}
        />
      )}
      <span className="sr-only" aria-live="polite">
        {copied ? t("linkCopied") : ""}
      </span>
    </>
  );
}
