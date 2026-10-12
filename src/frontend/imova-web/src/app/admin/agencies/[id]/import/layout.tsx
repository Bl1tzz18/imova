import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { Avatar } from "@/components/ui/Avatar";
import { Badge } from "@/components/ui/Badge";
import { VerifiedBadge } from "@/components/agency/VerifiedBadge";
import { getAgencyForMember } from "@/lib/api/agencies";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { agencyManagePath } from "@/lib/agency/manage";
import { importPageHref } from "@/lib/agency/importJobs";

// The CSV import of one agency — site admins only (IMOVA imports agencies' listings by hand; an agency
// writes to us from its Anunțuri tab). The agency's header, then the import pages. Reached from
// /admin/agencies ("Import CSV").
export default async function AdminAgencyImportLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const profile = await getCurrentUserProfile();
  if (!profile) {
    redirect(`/login?next=${encodeURIComponent(importPageHref(id))}`);
  }

  if (!profile.roles.includes("Admin")) {
    notFound();
  }

  const [agency, t] = await Promise.all([getAgencyForMember(id), getTranslations("AdminAgencies")]);
  if (!agency) {
    notFound();
  }

  return (
    <div className="flex min-h-screen flex-col">
      <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-10 sm:px-6 sm:py-12">
        <Link href="/admin/agencies" className="inline-flex items-center gap-1 text-sm font-medium text-ink-500 hover:text-ink-900">
          <span aria-hidden>←</span> {t("title")}
        </Link>

        <header className="mt-4 flex items-center gap-4">
          <Avatar
            userId={agency.id}
            displayName={agency.name}
            pictureUrl={agency.logoThumbnailUrl}
            size={48}
            shape="square"
            className="shrink-0 border border-ink-100"
          />
          <div className="min-w-0 flex-1">
            <h1 className="flex items-center gap-2 font-display text-xl font-medium text-ink-950 sm:text-2xl">
              <span className="truncate">{agency.name}</span>
              {agency.isVerified && <VerifiedBadge />}
            </h1>
            <p className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-ink-500">
              <Badge tone="brand">{t("adminOnly")}</Badge>
              <Link href={agencyManagePath(agency.id, "listings")} className="font-medium text-brand-700 hover:underline">
                {t("agencyListings")}
              </Link>
            </p>
          </div>
        </header>

        <div className="mt-8">{children}</div>
      </main>
      <Footer />
    </div>
  );
}
