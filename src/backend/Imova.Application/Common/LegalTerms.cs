namespace Imova.Application.Common;

// The version of the Terms of use + Privacy policy (/terms, /privacy) a new account accepts. It is the
// date of the text, and must equal the web app's LEGAL_VERSION (src/lib/legal/version.ts — a Vitest
// test checks the two agree). Bump both together whenever the text changes in substance.
public static class LegalTerms
{
    public const string CurrentVersion = "2026-11-22";
}
