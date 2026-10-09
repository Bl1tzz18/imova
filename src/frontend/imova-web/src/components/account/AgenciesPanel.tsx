"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Avatar } from "@/components/ui/Avatar";
import { Badge } from "@/components/ui/Badge";
import { Button, LinkButton } from "@/components/ui/Button";
import { VerifiedBadge } from "@/components/agency/VerifiedBadge";
import { agencyManagePath, NEW_AGENCY_HREF } from "@/lib/agency/manage";
import { respondToMyInvitation } from "@/lib/agency/manageActions";
import { formatDate } from "@/lib/utils/format";
import type { Invitation, MyAgency } from "@/types/agency";

// /account "Agențiile mele": invitations waiting for this account (accept / decline here), the
// agencies it belongs to (each opens its management page), and "Creează o agenție" — which needs a
// confirmed email first. Null lists: they couldn't be loaded.
export function AgenciesPanel({
  agencies,
  invitations,
  emailConfirmed,
}: {
  agencies: MyAgency[] | null;
  invitations: Invitation[] | null;
  emailConfirmed: boolean;
}) {
  const t = useTranslations("AgencyManage");

  return (
    <div className="flex flex-col gap-7">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <h2 className="font-display text-lg font-medium text-ink-950">{t("myAgenciesTitle")}</h2>
          <p className="mt-1 text-sm text-ink-500">{t("myAgenciesIntro")}</p>
        </div>
        {emailConfirmed && agencies && agencies.length > 0 && (
          <LinkButton href={NEW_AGENCY_HREF} variant="secondary" size="sm">
            {t("createAgency")}
          </LinkButton>
        )}
      </div>

      {invitations && invitations.length > 0 && (
        <section aria-labelledby="my-invitations-title">
          <h3 id="my-invitations-title" className="text-sm font-semibold text-ink-900">
            {t("myInvitationsTitle")}
          </h3>
          <ul className="mt-2 flex flex-col gap-2">
            {invitations.map((invitation) => (
              <InvitationCard key={invitation.id} invitation={invitation} />
            ))}
          </ul>
        </section>
      )}

      {agencies === null ? (
        <p className="text-sm text-accent-700">{t("loadFailed")}</p>
      ) : agencies.length > 0 ? (
        <ul className="flex flex-col divide-y divide-ink-100 rounded-2xl border border-ink-100">
          {agencies.map((agency) => (
            <li key={agency.id}>
              <Link
                href={agencyManagePath(agency.id)}
                className="flex items-center gap-3 px-3.5 py-3 transition-colors hover:bg-ink-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-600 sm:px-4"
              >
                <Avatar
                  userId={agency.id}
                  displayName={agency.name}
                  pictureUrl={agency.logoThumbnailUrl}
                  size={44}
                  shape="square"
                  className="shrink-0 border border-ink-100"
                />
                <span className="min-w-0 flex-1">
                  <span className="flex items-center gap-1.5">
                    <span className="truncate text-sm font-semibold text-ink-950">{agency.name}</span>
                    {agency.isVerified && <VerifiedBadge />}
                  </span>
                  <span className="mt-0.5 flex flex-wrap items-center gap-1.5 text-xs text-ink-500">
                    {t(`role.${agency.role}`)}
                    {agency.status === "Deactivated" && <Badge tone="accent">{t("deactivated")}</Badge>}
                  </span>
                </span>
                <span aria-hidden className="text-ink-300">
                  →
                </span>
              </Link>
            </li>
          ))}
        </ul>
      ) : (
        <div className="flex flex-col items-start gap-3 rounded-2xl border border-dashed border-ink-200 px-5 py-6">
          <p className="text-sm text-ink-600">{t("noAgencies")}</p>
          {emailConfirmed && <LinkButton href={NEW_AGENCY_HREF}>{t("createAgency")}</LinkButton>}
        </div>
      )}

      {!emailConfirmed && <p className="text-sm text-ink-500">{t("confirmEmailToCreate")}</p>}
    </div>
  );
}

function InvitationCard({ invitation }: { invitation: Invitation }) {
  const t = useTranslations("AgencyManage");
  const tInvitation = useTranslations("Invitation");
  const locale = useLocale();
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const role = invitation.role === "Admin" ? tInvitation("roleAdmin") : tInvitation("roleAgent");

  function answer(value: "accept" | "decline") {
    setError(null);
    startTransition(async () => {
      const result = await respondToMyInvitation(invitation.id, value);
      if (result.error) {
        setError(result.error);
        return;
      }
      router.refresh();
    });
  }

  return (
    <li className="rounded-2xl border border-brand-100 bg-brand-50/50 p-3.5 sm:p-4">
      <div className="flex items-start gap-3">
        <Avatar
          userId={invitation.agencyId}
          displayName={invitation.agencyName}
          pictureUrl={invitation.agencyLogoUrl}
          size={40}
          shape="square"
          className="shrink-0 border border-ink-100"
        />
        <div className="min-w-0 flex-1">
          <p className="text-sm text-ink-900">
            {invitation.invitedByName
              ? tInvitation("invitedBy", { name: invitation.invitedByName, agency: invitation.agencyName, role })
              : tInvitation("invited", { agency: invitation.agencyName, role })}
          </p>
          <p className="mt-0.5 text-xs text-ink-500">{t("invitationValidUntil", { date: formatDate(invitation.expiresAt, locale) })}</p>
          {!invitation.agencyIsActive && <p className="mt-1 text-xs text-accent-700">{tInvitation("agencyInactive")}</p>}
        </div>
      </div>
      <div className="mt-3 flex flex-wrap gap-2 sm:pl-[52px]">
        <Button type="button" size="sm" disabled={pending} onClick={() => answer("accept")}>
          {tInvitation("accept")}
        </Button>
        <Button type="button" size="sm" variant="secondary" disabled={pending} onClick={() => answer("decline")}>
          {tInvitation("decline")}
        </Button>
      </div>
      {error && (
        <p role="alert" className="mt-2 text-sm text-accent-700">
          {error}
        </p>
      )}
    </li>
  );
}
