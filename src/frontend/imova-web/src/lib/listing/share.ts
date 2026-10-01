// Sharing a listing — pure (Vitest-covered). The text goes in front of the link in a chat:
// "Apartament cu 2 camere, Botanica · 82 000 € · Chișinău, Botanica".

export function shareText(parts: (string | null | undefined)[]): string {
  return parts.filter((p): p is string => Boolean(p && p.trim())).join(" · ");
}

export type ShareTarget = "whatsapp" | "viber" | "telegram" | "facebook";

// Each app's own "share this" address. WhatsApp and Viber take one text (so the link goes at its
// end); Telegram takes the link and the text apart; Facebook only the link (it reads the page's
// own preview).
export function shareHref(target: ShareTarget, url: string, text: string): string {
  const both = `${text}\n${url}`;
  switch (target) {
    case "whatsapp":
      return `https://wa.me/?text=${encodeURIComponent(both)}`;
    case "viber":
      return `viber://forward?text=${encodeURIComponent(both)}`;
    case "telegram":
      return `https://t.me/share/url?url=${encodeURIComponent(url)}&text=${encodeURIComponent(text)}`;
    case "facebook":
      return `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(url)}`;
  }
}
