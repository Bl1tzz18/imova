"use client";

import { useTranslations } from "next-intl";
import { Button, LinkButton } from "@/components/ui/Button";

// A page that failed to render, in the visitor's language, under the site's header. The server side
// of the error is already in Application Insights (instrumentation.ts, onRequestError); the digest
// is what links what the visitor saw to that entry.
export default function PageError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  const t = useTranslations("Meta");

  return (
    <main className="flex flex-1 items-center justify-center px-4 py-24 text-center">
      <div className="flex flex-col items-center gap-3">
        <h1 className="font-display text-2xl font-medium text-ink-950">{t("errorTitle")}</h1>
        <p className="max-w-sm text-sm text-ink-500">{t("errorText")}</p>
        <div className="mt-2 flex flex-wrap justify-center gap-2">
          <Button size="sm" onClick={reset}>
            {t("tryAgain")}
          </Button>
          <LinkButton href="/" size="sm" variant="secondary">
            {t("backHome")}
          </LinkButton>
        </div>
        {error.digest && <p className="mt-4 font-mono text-xs text-ink-400">{error.digest}</p>}
      </div>
    </main>
  );
}
