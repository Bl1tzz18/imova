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

// The heart over a listing's photo — on its card ("md") and on the listing page's main photo ("lg",
// beside "Distribuie").
export function SaveListingButton({
  listingId,
  initialSaved,
  size = "md",
  className,
}: {
  listingId: string;
  initialSaved: boolean;
  size?: "md" | "lg";
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
      const result = await setFavorite(listingId, next, pathname);
      if (result.error) {
        setSaved(!next);
      }
    });
  }

  return (
    <button
      type="button"
      onClick={handleClick}
      disabled={pending}
      aria-pressed={saved}
      aria-label={saved ? t("savedListing") : t("saveListing")}
      className={cn(
        "flex items-center justify-center rounded-full bg-white/90 shadow-sm backdrop-blur transition-colors hover:bg-white disabled:opacity-70",
        size === "lg" ? "h-10 w-10" : "h-9 w-9",
        saved ? "text-accent-600" : "text-ink-600",
        className,
      )}
    >
      <HeartIcon filled={saved} className="h-[18px] w-[18px]" />
    </button>
  );
}
