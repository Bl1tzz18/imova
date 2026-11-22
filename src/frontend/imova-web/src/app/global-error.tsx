"use client";

import "./globals.css";

// The site's own layout failed (rare), so there is no header, no language and no translations:
// plain Romanian with its own <html>, like app/not-found.tsx.
export default function GlobalError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <html lang="ro">
      <body>
        <main className="flex min-h-screen items-center justify-center px-4 text-center">
          <div className="flex flex-col items-center gap-3">
            <h1 className="font-display text-2xl font-medium text-ink-950">Ceva nu a mers</h1>
            <p className="max-w-sm text-sm text-ink-500">A apărut o eroare la noi. Încearcă din nou peste câteva momente.</p>
            <button type="button" onClick={reset} className="mt-2 text-sm font-medium text-brand-700 hover:underline">
              Încearcă din nou
            </button>
            {error.digest && <p className="mt-4 font-mono text-xs text-ink-400">{error.digest}</p>}
          </div>
        </main>
      </body>
    </html>
  );
}
