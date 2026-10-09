import { useTranslations } from "next-intl";
import { Avatar } from "@/components/ui/Avatar";
import { VerifiedBadge } from "@/components/agency/VerifiedBadge";
import { formatPhone } from "@/lib/listing/contactCard";
import { websiteLabel } from "@/lib/agency/publicPage";

// How the agency shows on IMOVA, from what's in the profile form right now: the box on its listings'
// contact card (logo, name, badge, link to its page), then the contact lines of its own page.
export function AgencyPreviewCard({
  id,
  name,
  logoUrl,
  isVerified,
  city,
  phone,
  email,
  website,
}: {
  id: string;
  name: string;
  logoUrl: string | null;
  isVerified: boolean;
  city: string | null;
  phone: string;
  email: string;
  website: string;
}) {
  const t = useTranslations("AgencyManage");
  const tAgencies = useTranslations("Agencies");
  const shownName = name.trim() || t("previewNamePlaceholder");

  return (
    <div className="rounded-2xl border border-ink-100 bg-white p-4 shadow-[var(--shadow-card)]">
      <div className="flex items-center gap-3 rounded-xl border border-ink-100 px-3 py-2.5">
        <Avatar userId={id} displayName={shownName} pictureUrl={logoUrl} size={40} shape="square" className="shrink-0 border border-ink-100" />
        <span className="min-w-0 flex-1">
          <span className="flex items-center gap-1.5">
            <span className="truncate text-sm font-semibold text-ink-950">{shownName}</span>
            {isVerified && <VerifiedBadge />}
          </span>
          <span className="block text-sm text-accent-700">{tAgencies("seeAllListings")}</span>
        </span>
      </div>

      <dl className="mt-3 flex flex-col gap-1.5 text-sm">
        {city && (
          <div className="flex gap-2">
            <dt className="sr-only">{t("cityLabel")}</dt>
            <dd className="text-ink-600">{city}</dd>
          </div>
        )}
        {phone.trim() && (
          <div className="flex gap-2">
            <dt className="sr-only">{t("phoneLabel")}</dt>
            <dd className="font-medium tabular-nums text-ink-900">{formatPhone(phone)}</dd>
          </div>
        )}
        {email.trim() && (
          <div className="flex min-w-0 gap-2">
            <dt className="sr-only">{t("emailLabel")}</dt>
            <dd className="truncate text-ink-600">{email.trim()}</dd>
          </div>
        )}
        {website.trim() && (
          <div className="flex min-w-0 gap-2">
            <dt className="sr-only">{t("websiteLabel")}</dt>
            <dd className="truncate text-brand-700">{websiteLabel(website.trim())}</dd>
          </div>
        )}
      </dl>
      <p className="mt-3 text-xs text-ink-400">{t("previewPhoneNote")}</p>
    </div>
  );
}
