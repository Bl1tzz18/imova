import type { ReactNode } from "react";
import { Link } from "@/i18n/navigation";

// The <terms>/<privacy> tags of "Auth.termsAgreement" and "Auth.googleTermsNotice": links to the two
// pages, opened in a new tab so a half-filled sign-up form isn't lost.
const linkClass = "font-medium text-brand-700 underline-offset-2 hover:underline";

export const legalLinks = {
  terms: (chunks: ReactNode) => (
    <Link href="/terms" target="_blank" className={linkClass}>
      {chunks}
    </Link>
  ),
  privacy: (chunks: ReactNode) => (
    <Link href="/privacy" target="_blank" className={linkClass}>
      {chunks}
    </Link>
  ),
};
