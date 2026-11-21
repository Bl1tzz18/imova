// Where to send someone after signing in: only ever a path on this site. `next` comes from the query
// string or a form field, so it's attacker-controlled — an absolute URL, or a path that browsers read
// as another host ("//evil.com", "/\evil.com"), would make the redirect an open redirect.
export function safeNextPath(next: unknown): string {
  if (typeof next !== "string" || !next.startsWith("/")) return "/";
  if (next.startsWith("//") || next.startsWith("/\\")) return "/";
  // Browsers drop tabs and newlines inside URLs, so "/\t/evil.com" would become "//evil.com".
  if (/[\u0000-\u001f]/.test(next)) return "/";
  return next;
}
