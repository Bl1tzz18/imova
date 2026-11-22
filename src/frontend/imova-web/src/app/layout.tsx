// The real root layout (<html>, fonts, header) is app/[locale]/layout.tsx — the language is part of
// every address. This one only exists because app/not-found.tsx (an address outside any language)
// needs a layout above it.
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return children;
}
