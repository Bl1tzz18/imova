export default async function ProprietatePage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;

  return (
    <main style={{ fontFamily: "sans-serif", padding: "2rem" }}>
      <h1>Proprietate: {slug}</h1>
    </main>
  );
}
