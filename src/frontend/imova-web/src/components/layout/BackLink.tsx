"use client";

import { useEffect, useState, type MouseEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { canGoBackTo, LAST_SEARCH_KEY, PREVIOUS_URL_KEY } from "@/lib/navigation/history";

function stored(key: string): string | null {
  try {
    return sessionStorage.getItem(key);
  } catch {
    return null;
  }
}

// "← Înapoi …". A real link to `href` — or, with `toLastSearch`, to the visitor's last search when
// there is one (href is then the fallback). When that's exactly the page they came from, it's the
// browser's Back instead, so the page comes back as they left it (scroll position included) and no
// extra history entry piles up.
export function BackLink({ href, label, toLastSearch = false }: { href: string; label: string; toLastSearch?: boolean }) {
  const router = useRouter();
  const [target, setTarget] = useState(href);

  useEffect(() => {
    setTarget((toLastSearch && stored(LAST_SEARCH_KEY)) || href);
  }, [href, toLastSearch]);

  function handleClick(e: MouseEvent<HTMLAnchorElement>) {
    // A new tab / window keeps the plain link.
    if (e.metaKey || e.ctrlKey || e.shiftKey || e.button !== 0) return;
    if (canGoBackTo(target, stored(PREVIOUS_URL_KEY))) {
      e.preventDefault();
      router.back();
    }
  }

  return (
    <Link
      href={target}
      onClick={handleClick}
      className="inline-flex items-center gap-1.5 text-sm font-medium text-ink-500 transition-colors hover:text-ink-900"
    >
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="h-4 w-4" aria-hidden>
        <path d="m15 18-6-6 6-6" />
      </svg>
      {label}
    </Link>
  );
}
