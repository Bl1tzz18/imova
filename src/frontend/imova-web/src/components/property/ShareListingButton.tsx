"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils/cn";

function ShareIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className={className}>
      <circle cx="18" cy="5" r="2.6" />
      <circle cx="6" cy="12" r="2.6" />
      <circle cx="18" cy="19" r="2.6" />
      <path d="M8.3 10.6l7.4-4.2M8.3 13.4l7.4 4.2" />
    </svg>
  );
}

// Icon-only share button, styled to match SaveListingButton's circular icon variant — the app
// has no existing share affordance to reuse, so this is new but deliberately copies that
// component's shape/size so the two sit consistently side by side on a card.
export function ShareListingButton({ propertyId, className }: { propertyId: string; className?: string }) {
  const t = useTranslations("PropertyCard");
  const [copied, setCopied] = useState(false);

  async function handleClick(e: React.MouseEvent) {
    // Cards wrap the whole thing in a <Link> — don't let the click navigate.
    e.preventDefault();
    e.stopPropagation();

    const url = `${window.location.origin}/property/${propertyId}`;

    if (typeof navigator.share === "function") {
      try {
        await navigator.share({ url });
      } catch {
        // User cancelled the native share sheet — nothing to recover from.
      }
      return;
    }

    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      // Clipboard access unavailable — nothing to recover from silently.
    }
  }

  return (
    <button
      type="button"
      onClick={handleClick}
      aria-label={t(copied ? "linkCopied" : "share")}
      title={t(copied ? "linkCopied" : "share")}
      className={cn(
        "flex h-9 w-9 items-center justify-center rounded-full bg-white/90 text-ink-600 shadow-sm backdrop-blur transition-colors hover:bg-white",
        className,
      )}
    >
      <ShareIcon className="h-[18px] w-[18px]" />
    </button>
  );
}
