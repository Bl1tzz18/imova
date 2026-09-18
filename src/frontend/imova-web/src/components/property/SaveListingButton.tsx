"use client";

import { useState, useTransition } from "react";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";
import { setFavorite } from "@/lib/property/actions";
import { cn } from "@/lib/utils/cn";

function HeartIcon({ filled, className }: { filled: boolean; className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill={filled ? "currentColor" : "none"}
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M12 21s-7.5-4.6-10-9.3C.5 8.1 2.4 4.5 6 4c2.1-.3 4 .8 6 3 2-2.2 3.9-3.3 6-3 3.6.5 5.5 4.1 4 7.7C19.5 16.4 12 21 12 21Z" />
    </svg>
  );
}

// Icon-only (listing cards) or icon+label (listing detail page) — same toggle logic either way.
export function SaveListingButton({
  propertyId,
  initialSaved,
  variant = "icon",
  className,
}: {
  propertyId: string;
  initialSaved: boolean;
  variant?: "icon" | "labeled";
  className?: string;
}) {
  const t = useTranslations("PropertyCard");
  const pathname = usePathname();
  const [saved, setSaved] = useState(initialSaved);
  const [pending, startTransition] = useTransition();

  function handleClick(e: React.MouseEvent) {
    // Cards wrap the whole thing in a <Link> — don't let the click navigate.
    e.preventDefault();
    e.stopPropagation();

    const next = !saved;
    setSaved(next);

    startTransition(async () => {
      const result = await setFavorite(propertyId, next, pathname);
      if (result.error) {
        setSaved(!next);
      }
    });
  }

  if (variant === "labeled") {
    return (
      <button
        type="button"
        onClick={handleClick}
        disabled={pending}
        aria-pressed={saved}
        className={cn(
          "inline-flex items-center gap-2 rounded-full border px-4 py-2.5 text-sm font-medium transition-colors disabled:opacity-60",
          saved
            ? "border-accent-200 bg-accent-50 text-accent-700 hover:bg-accent-100"
            : "border-ink-200 bg-white text-ink-700 hover:bg-ink-50",
          className,
        )}
      >
        <HeartIcon filled={saved} className="h-4 w-4" />
        {saved ? t("savedListing") : t("saveListing")}
      </button>
    );
  }

  return (
    <button
      type="button"
      onClick={handleClick}
      disabled={pending}
      aria-pressed={saved}
      aria-label={saved ? t("savedListing") : t("saveListing")}
      className={cn(
        "flex h-9 w-9 items-center justify-center rounded-full bg-white/90 shadow-sm backdrop-blur transition-colors hover:bg-white disabled:opacity-70",
        saved ? "text-accent-600" : "text-ink-600",
        className,
      )}
    >
      <HeartIcon filled={saved} className="h-[18px] w-[18px]" />
    </button>
  );
}
