import type { EurRates } from "@/lib/listing/price";

// The backend's exchange rates (configuration — they rarely change), cached for an hour. Null when
// the call fails: the converted price is an extra and is then simply left out.
export async function getExchangeRates(): Promise<EurRates | null> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  try {
    const res = await fetch(`${apiUrl}/api/v1/exchange-rates`, { next: { revalidate: 3600 } });
    return res.ok ? ((await res.json()) as { eurPerUnit: EurRates }).eurPerUnit : null;
  } catch {
    return null;
  }
}
