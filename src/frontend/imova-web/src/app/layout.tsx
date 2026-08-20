export const metadata = {
  title: "IMOVA",
  description: "Imobiliare de la persoane fizice, in Moldova",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="ro">
      <body>{children}</body>
    </html>
  );
}
