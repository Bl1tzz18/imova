"use client";

import { useEffect, useRef, useState, useTransition } from "react";
import { useTranslations } from "next-intl";
import { fetchGoogleClientId } from "@/lib/api/authConfig";
import { googleLogin } from "@/lib/auth/actions";

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: {
            client_id: string;
            callback: (response: { credential: string }) => void;
          }) => void;
          renderButton: (parent: HTMLElement, options: Record<string, unknown>) => void;
        };
      };
    };
  }
}

// Cached at module scope, not component state — navigating away from /login and back shouldn't
// re-insert or re-await the script tag once the browser already has it loaded.
let gsiScriptPromise: Promise<void> | null = null;

function loadGoogleScript(): Promise<void> {
  if (typeof window !== "undefined" && window.google) {
    return Promise.resolve();
  }
  gsiScriptPromise ??= new Promise((resolve, reject) => {
    const script = document.createElement("script");
    script.src = "https://accounts.google.com/gsi/client";
    script.async = true;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error("Failed to load the Google Sign-In script."));
    document.head.appendChild(script);
  });
  return gsiScriptPromise;
}

// Fetches its own config in the browser on every mount, rather than receiving it as a
// server-rendered prop from an async Server Component. A value fetched server-side here would
// only be as fresh as Next.js's App Router decides to make the *page's* server render on a given
// visit — client-side navigations can be served from an already-rendered/prefetched RSC payload
// instead of re-running that fetch, which is why the button used to only show up after a hard
// refresh. Fetching from this already-client component's own effect ties it to this component's
// mount instead, so it's correct however the user arrives at the page.
export function GoogleSignInButton({ next }: { next?: string }) {
  const t = useTranslations("Auth");
  const buttonRef = useRef<HTMLDivElement>(null);
  const [clientId, setClientId] = useState("");
  const [scriptReady, setScriptReady] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [, startTransition] = useTransition();

  useEffect(() => {
    let cancelled = false;

    fetchGoogleClientId().then((id) => {
      if (cancelled || !id) return;
      setClientId(id);
      loadGoogleScript().then(() => {
        if (!cancelled) setScriptReady(true);
      });
    });

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!clientId || !scriptReady || !buttonRef.current || !window.google) {
      return;
    }
    window.google.accounts.id.initialize({ client_id: clientId, callback: handleCredential });
    window.google.accounts.id.renderButton(buttonRef.current, {
      theme: "outline",
      size: "large",
      width: 336,
      text: "continue_with",
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [clientId, scriptReady]);

  function handleCredential(response: { credential: string }) {
    startTransition(async () => {
      const result = await googleLogin(response.credential, next);
      if (result?.error) {
        setError(result.error);
      }
    });
  }

  if (!clientId) {
    return null;
  }

  return (
    <>
      <div className="my-5 flex items-center gap-3">
        <div className="h-px flex-1 bg-ink-100" />
        <span className="text-xs font-medium uppercase tracking-wide text-ink-400">{t("orDivider")}</span>
        <div className="h-px flex-1 bg-ink-100" />
      </div>
      <div ref={buttonRef} className="flex justify-center" />
      {error && <p className="mt-2 text-center text-sm text-accent-600">{error}</p>}
    </>
  );
}
