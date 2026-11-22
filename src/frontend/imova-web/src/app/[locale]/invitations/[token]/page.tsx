import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { AuthCard, AuthNotice } from "@/components/auth/AuthCard";
import { InvitationResponse } from "@/components/agency/InvitationResponse";
import { Avatar } from "@/components/ui/Avatar";
import { Badge } from "@/components/ui/Badge";
import { Button, LinkButton } from "@/components/ui/Button";
import { getInvitation } from "@/lib/agency/invitations";
import { invitationPath, invitationView } from "@/lib/agency/invitationPage";
import { logoutTo } from "@/lib/auth/actions";
import { getCurrentUserProfile } from "@/lib/auth/profile";

export const metadata: Metadata = { robots: { index: false } };

// Reached from an agency invitation email. Opening it does nothing by itself (mail scanners open
// links): accepting or declining takes a button.
export default async function InvitationPage({ params }: { params: Promise<{ token: string }> }) {
  const { token } = await params;
  const t = await getTranslations("Invitation");
  const [invitation, profile] = await Promise.all([getInvitation(token), getCurrentUserProfile()]);
  const view = invitationView(invitation, profile?.email ?? null);
  const back = invitationPath(token);

  if (!invitation || view === "notFound") {
    return (
      <AuthCard title={t("title")}>
        <div className="flex flex-col gap-4">
          <AuthNotice tone="error">{t("notFound")}</AuthNotice>
          <LinkButton href="/" variant="secondary" className="w-full">
            {t("goHome")}
          </LinkButton>
        </div>
      </AuthCard>
    );
  }

  const role = invitation.role === "Admin" ? t("roleAdmin") : t("roleAgent");

  return (
    <AuthCard title={t("title")}>
      <div className="flex items-center gap-3.5">
        {/* The name is right beside it, so the logo itself is decorative. */}
        <Avatar
          userId={invitation.agencyId}
          displayName={invitation.agencyName}
          pictureUrl={invitation.agencyLogoUrl}
          size={56}
          className="shrink-0"
        />
        <div className="min-w-0">
          <p className="text-base font-semibold text-ink-950 [overflow-wrap:anywhere]">{invitation.agencyName}</p>
          {invitation.agencyIsVerified && (
            <Badge tone="success" className="mt-1">
              {t("verified")}
            </Badge>
          )}
        </div>
      </div>

      <p className="mt-4 text-sm leading-6 text-ink-700">
        {invitation.invitedByName
          ? t("invitedBy", { name: invitation.invitedByName, agency: invitation.agencyName, role })
          : t("invited", { agency: invitation.agencyName, role })}
      </p>
      <p className="mt-1 text-sm text-ink-500">{invitation.role === "Admin" ? t("adminMeans") : t("agentMeans")}</p>

      <div className="mt-6">
        {view === "expired" && <AuthNotice tone="error">{t("expired")}</AuthNotice>}
        {view === "closed" && <AuthNotice tone="error">{t("closed")}</AuthNotice>}

        {view === "signIn" && (
          <div className="flex flex-col gap-3">
            <p className="text-sm text-ink-700">{t("signInFirst", { email: invitation.email })}</p>
            <LinkButton href={`/login?next=${encodeURIComponent(back)}`} className="w-full">
              {t("signIn")}
            </LinkButton>
            <LinkButton href={`/register?next=${encodeURIComponent(back)}`} variant="secondary" className="w-full">
              {t("register")}
            </LinkButton>
          </div>
        )}

        {view === "wrongAccount" && (
          <div className="flex flex-col gap-3">
            <AuthNotice tone="error">{t("wrongAccount", { email: invitation.email, current: profile?.email ?? "" })}</AuthNotice>
            <form action={logoutTo.bind(null, back)}>
              <Button type="submit" className="w-full">
                {t("switchAccount")}
              </Button>
            </form>
          </div>
        )}

        {view === "ready" && (
          <div className="flex flex-col gap-4">
            {!invitation.agencyIsActive && <AuthNotice tone="error">{t("agencyInactive")}</AuthNotice>}
            <InvitationResponse token={token} agencyName={invitation.agencyName} />
          </div>
        )}
      </div>
    </AuthCard>
  );
}
