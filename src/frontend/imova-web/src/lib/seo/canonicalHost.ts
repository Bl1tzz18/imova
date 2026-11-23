// www.imova.md is bound to the web app too (people type it), but the site lives at imova.md: a
// request for the www name is sent, permanently, to the same path on the site's own address. Any
// other host (the app's *.azurecontainerapps.io address, localhost) is left alone.
export function wwwRedirect(host: string | null, siteUrl: string, pathname: string, search: string): string | null {
  if (!host) return null;
  const site = new URL(siteUrl);
  const requested = host.toLowerCase().replace(/:\d+$/, "");
  if (requested !== `www.${site.hostname.toLowerCase()}`) return null;
  return `${site.origin}${pathname}${search}`;
}
