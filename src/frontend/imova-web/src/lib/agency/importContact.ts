// Importing an agency's listings is a service IMOVA does by hand: the agency writes to this address
// (the Anunțuri tab's "Importă anunțurile tale"). The one place the address lives.
// Before launch: replace it with an @imova.md address (docs/agency-progress.md).
export const IMPORT_CONTACT_EMAIL = "catalinfilip47@gmail.com";

// The "Scrie-ne" button: a mailto: link with the agency named in the subject, so the email says who
// is writing. Pure (Vitest-covered).
export function importContactMailto(subject: string): string {
  return `mailto:${IMPORT_CONTACT_EMAIL}?subject=${encodeURIComponent(subject)}`;
}
