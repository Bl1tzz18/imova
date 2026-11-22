import "./globals.css";

// A 404 outside any language (rare: middleware puts every page address under one). Plain Romanian,
// with its own <html> since there is no layout with one above it.
export default function GlobalNotFound() {
  return (
    <html lang="ro">
      <body>
        <main className="flex min-h-screen items-center justify-center px-4 text-center">
          <div className="flex flex-col items-center gap-3">
            <h1 className="font-display text-2xl font-medium text-ink-950">Pagina nu a fost găsită</h1>
            <a href="/" className="text-sm font-medium text-brand-700 hover:underline">
              Înapoi la prima pagină
            </a>
          </div>
        </main>
      </body>
    </html>
  );
}
