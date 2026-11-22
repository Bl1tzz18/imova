// The account's Telegram connection (GET /api/v1/users/me/telegram). available: the site has a
// bot (otherwise the Notifications tab shows no Telegram block). Plain types and helpers only, so
// client components can import them; the API calls are server actions in lib/account/actions.ts.
export type TelegramConnection = {
  available: boolean;
  connected: boolean;
  username: string | null;
  firstName: string | null;
  connectedAt: string | null;
};

// "Conectează Telegram": the t.me link (it carries a one-time code) and when it stops working.
export type TelegramLinkStart = { url: string; expiresAt: string };

// How the connected chat is named: "@username", else the first name Telegram gave, else nothing.
export function telegramDisplayName(connection: Pick<TelegramConnection, "username" | "firstName">): string | null {
  if (connection.username) return `@${connection.username}`;
  return connection.firstName?.trim() || null;
}

// Whether a link from "Conectează Telegram" can still be used.
export function telegramLinkExpired(link: TelegramLinkStart, now: Date = new Date()): boolean {
  return new Date(link.expiresAt).getTime() <= now.getTime();
}
