"use client";

import { startTransition, useActionState, useEffect, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Avatar } from "@/components/ui/Avatar";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { FieldLabel, SelectInput, TextInput } from "@/components/ui/Field";
import { canRemove, defaultHeir, heirOptions, inviteRoles, roleChoices } from "@/lib/agency/manage";
import {
  changeMemberRole,
  inviteMember,
  removeMember,
  resendInvitation,
  revokeInvitation,
  type InviteState,
} from "@/lib/agency/manageActions";
import { formatDate } from "@/lib/utils/format";
import type { AgencyInvitation, AgencyMember, AgencyRole } from "@/types/agency";

export type MembersActor = { userId: string; role: AgencyRole | null; isSiteAdmin: boolean };

// The Membri tab: who's in the agency (with role changes and removal for those allowed), an invite
// form, and the invitations still open (resend, revoke, a warning when the email didn't go out).
// Agents see the list only. Every change refreshes the page from the server.
export function MembersPanel({
  agencyId,
  members,
  invitations,
  actor,
}: {
  agencyId: string;
  members: AgencyMember[];
  // Null for an Agent (they can't see them).
  invitations: AgencyInvitation[] | null;
  actor: MembersActor;
}) {
  const t = useTranslations("AgencyManage");
  const roles = inviteRoles(actor.role, actor.isSiteAdmin);

  return (
    <div className="flex flex-col gap-8">
      <section aria-labelledby="agency-members-title">
        <h2 id="agency-members-title" className="font-display text-lg font-medium text-ink-950">
          {t("membersTitle", { count: members.length })}
        </h2>
        <ul className="mt-3 flex flex-col divide-y divide-ink-100 rounded-2xl border border-ink-100 bg-white">
          {members.map((member) => (
            <MemberRow key={member.userId} agencyId={agencyId} member={member} members={members} actor={actor} />
          ))}
        </ul>
        <p className="mt-2 text-xs text-ink-500">{t("rolesExplained")}</p>
      </section>

      {roles.length > 0 && <InviteForm agencyId={agencyId} roles={roles} />}

      {invitations && invitations.length > 0 && (
        <section aria-labelledby="agency-invitations-title">
          <h2 id="agency-invitations-title" className="font-display text-lg font-medium text-ink-950">
            {t("invitationsTitle", { count: invitations.length })}
          </h2>
          <ul className="mt-3 flex flex-col divide-y divide-ink-100 rounded-2xl border border-ink-100 bg-white">
            {invitations.map((invitation) => (
              <InvitationRow key={invitation.id} agencyId={agencyId} invitation={invitation} />
            ))}
          </ul>
        </section>
      )}
    </div>
  );
}

function MemberRow({
  agencyId,
  member,
  members,
  actor,
}: {
  agencyId: string;
  member: AgencyMember;
  members: AgencyMember[];
  actor: MembersActor;
}) {
  const t = useTranslations("AgencyManage");
  const locale = useLocale();
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const [selfRole, setSelfRole] = useState<AgencyRole | null>(null);
  const [removing, setRemoving] = useState(false);
  const isMe = member.userId === actor.userId;
  const choices = roleChoices(actor, member, members);
  const removable = canRemove(actor, member, members);

  function change(role: AgencyRole) {
    setError(null);
    startTransition(async () => {
      const result = await changeMemberRole(agencyId, member.userId, role);
      if (result.error) {
        setError(result.error);
        return;
      }
      setSelfRole(null);
      router.refresh();
    });
  }

  return (
    <li className="flex flex-col gap-3 p-3.5 sm:flex-row sm:flex-wrap sm:items-center sm:p-4">
      <div className="flex min-w-0 flex-1 items-center gap-3">
        <Avatar userId={member.userId} displayName={member.name} email={member.email} pictureUrl={member.pictureUrl} size={40} />
        <div className="min-w-0">
          <p className="truncate text-sm font-medium text-ink-950">
            {member.name}
            {isMe && <span className="font-normal text-ink-500"> {t("you")}</span>}
          </p>
          <p className="truncate text-xs text-ink-500">{member.email}</p>
          <p className="text-xs text-ink-400">
            {t("memberSince", { date: formatDate(member.joinedAt, locale) })}
            {" · "}
            {t("listingCount", { count: member.listingCount })}
          </p>
        </div>
      </div>

      <div className="flex items-center gap-2 sm:shrink-0">
        {choices.length > 0 ? (
          <label className="min-w-0 flex-1 sm:w-40 sm:flex-none">
            <span className="sr-only">{t("roleOf", { name: member.name })}</span>
            <SelectInput
              value={member.role}
              disabled={pending}
              onChange={(e) => {
                const role = e.target.value as AgencyRole;
                if (isMe) setSelfRole(role);
                else change(role);
              }}
              className="h-10"
            >
              {choices.map((role) => (
                <option key={role} value={role}>
                  {t(`role.${role}`)}
                </option>
              ))}
            </SelectInput>
          </label>
        ) : (
          <Badge tone={member.role === "Agent" ? "neutral" : "brand"}>{t(`role.${member.role}`)}</Badge>
        )}

        {removable && (
          <Button type="button" variant="secondary" size="sm" disabled={pending} onClick={() => setRemoving(true)}>
            {t("remove")}
          </Button>
        )}
      </div>

      {error && selfRole === null && (
        <p role="alert" className="text-sm text-accent-700 sm:basis-full">
          {error}
        </p>
      )}

      <ConfirmDialog
        open={selfRole !== null}
        title={t("stepDownTitle")}
        confirmLabel={t("stepDownConfirm")}
        cancelLabel={t("cancel")}
        pendingLabel={t("saving")}
        pending={pending}
        error={selfRole !== null ? error : null}
        onConfirm={() => selfRole && change(selfRole)}
        onCancel={() => {
          setSelfRole(null);
          setError(null);
        }}
      >
        {selfRole && t("stepDownBody", { role: t(`role.${selfRole}`) })}
      </ConfirmDialog>

      {removing && (
        <RemoveMemberDialog
          agencyId={agencyId}
          member={member}
          members={members}
          actorId={actor.userId}
          onClose={() => setRemoving(false)}
        />
      )}
    </li>
  );
}

// "Remove X?" — and when they wrote listings for the agency, who takes them over (an Owner or Admin
// who stays; the one removing them by default). The listings stay with the agency either way.
function RemoveMemberDialog({
  agencyId,
  member,
  members,
  actorId,
  onClose,
}: {
  agencyId: string;
  member: AgencyMember;
  members: AgencyMember[];
  actorId: string;
  onClose: () => void;
}) {
  const t = useTranslations("AgencyManage");
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const heirs = heirOptions(members, member.userId);
  const [heir, setHeir] = useState(() => defaultHeir(members, member.userId, actorId) ?? "");

  function confirm() {
    setError(null);
    startTransition(async () => {
      const result = await removeMember(agencyId, member.userId, member.listingCount > 0 ? heir || null : null);
      if (result.error) {
        setError(result.error);
        return;
      }
      onClose();
      router.refresh();
    });
  }

  return (
    <ConfirmDialog
      open
      danger
      title={t("removeTitle", { name: member.name })}
      confirmLabel={t("removeConfirm")}
      cancelLabel={t("cancel")}
      pendingLabel={t("removing")}
      pending={pending}
      error={error}
      onConfirm={confirm}
      onCancel={onClose}
    >
      <HeirChoice listingCount={member.listingCount} heirs={heirs} heir={heir} onChange={setHeir} />
    </ConfirmDialog>
  );
}

// What happens to the listings of someone leaving the agency, and — when there are any — the choice
// of who takes them over. Shared by removing a member and leaving.
export function HeirChoice({
  listingCount,
  heirs,
  heir,
  onChange,
  self = false,
}: {
  listingCount: number;
  heirs: AgencyMember[];
  heir: string;
  onChange: (userId: string) => void;
  self?: boolean;
}) {
  const t = useTranslations("AgencyManage");

  if (listingCount === 0) {
    return <p>{self ? t("leaveNoListings") : t("removeNoListings")}</p>;
  }

  return (
    <div className="flex flex-col gap-3">
      <p>{self ? t("leaveListings", { count: listingCount }) : t("removeListings", { count: listingCount })}</p>
      <label className="block">
        <FieldLabel>{t("heirLabel")}</FieldLabel>
        <SelectInput value={heir} onChange={(e) => onChange(e.target.value)}>
          {heirs.map((m) => (
            <option key={m.userId} value={m.userId}>
              {m.name} · {t(`role.${m.role}`)}
            </option>
          ))}
        </SelectInput>
      </label>
      <p className="text-xs text-ink-500">{t("conversationsStay")}</p>
    </div>
  );
}

const inviteInitialState: InviteState = {};

function InviteForm({ agencyId, roles }: { agencyId: string; roles: AgencyRole[] }) {
  const t = useTranslations("AgencyManage");
  const router = useRouter();
  const [state, formAction, pending] = useActionState(inviteMember, inviteInitialState);
  // A refused address stays in the field; a sent one is cleared for the next.
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<AgencyRole>("Agent");

  // Sent: the new invitation shows in the list below; the field is free for the next one.
  useEffect(() => {
    if (!state.sentAt) return;
    setEmail("");
    router.refresh();
  }, [state.sentAt, router]);

  return (
    <section aria-labelledby="agency-invite-title" className="rounded-2xl border border-ink-100 bg-ink-50/50 p-4 sm:p-5">
      <h2 id="agency-invite-title" className="font-display text-lg font-medium text-ink-950">
        {t("inviteTitle")}
      </h2>
      <p className="mt-1 text-sm text-ink-500">{t("inviteIntro")}</p>

      <form
        onSubmit={(e) => {
          // Not `action=`: React's reset after it would put the (controlled) role back to the first option.
          e.preventDefault();
          const data = new FormData(e.currentTarget);
          startTransition(() => formAction(data));
        }}
        className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-end"
      >
        <input type="hidden" name="agencyId" value={agencyId} />
        <label className="block min-w-0 flex-1">
          <FieldLabel required>{t("inviteEmail")}</FieldLabel>
          <TextInput
            type="email"
            name="email"
            required
            autoComplete="off"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder={t("inviteEmailPlaceholder")}
          />
        </label>
        {roles.length > 1 ? (
          <label className="block sm:w-44">
            <FieldLabel>{t("inviteRole")}</FieldLabel>
            <SelectInput name="role" value={role} onChange={(e) => setRole(e.target.value as AgencyRole)}>
              {roles.map((r) => (
                <option key={r} value={r}>
                  {t(`role.${r}`)}
                </option>
              ))}
            </SelectInput>
          </label>
        ) : (
          <input type="hidden" name="role" value={roles[0]} />
        )}
        <Button type="submit" disabled={pending}>
          {pending ? t("inviteSending") : t("inviteSubmit")}
        </Button>
      </form>

      {state.error && (
        <p role="alert" className="mt-3 text-sm text-accent-700">
          {state.error}
        </p>
      )}
      {state.sentTo && !state.error && (
        <p role="status" className="mt-3 text-sm text-brand-700">
          {t("inviteSent", { email: state.sentTo })}
        </p>
      )}
    </section>
  );
}

function InvitationRow({ agencyId, invitation }: { agencyId: string; invitation: AgencyInvitation }) {
  const t = useTranslations("AgencyManage");
  const locale = useLocale();
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const [revoking, setRevoking] = useState(false);
  const [resent, setResent] = useState(false);
  const expired = invitation.status === "Expired";

  function run(action: () => Promise<{ error?: string }>, after?: () => void) {
    setError(null);
    startTransition(async () => {
      const result = await action();
      if (result.error) {
        setError(result.error);
        return;
      }
      after?.();
      router.refresh();
    });
  }

  return (
    <li className="flex flex-col gap-2 p-3.5 sm:p-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium text-ink-950">{invitation.email}</p>
          <p className="text-xs text-ink-500">
            {t(`role.${invitation.role}`)}
            {" · "}
            {expired ? t("invitationExpired") : t("invitationSent", { date: formatDate(invitation.lastSentAt, locale) })}
            {invitation.invitedByName && <> · {t("invitedBy", { name: invitation.invitedByName })}</>}
          </p>
        </div>
        <div className="flex gap-2 sm:shrink-0">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            disabled={pending}
            onClick={() => run(() => resendInvitation(agencyId, invitation.id), () => setResent(true))}
          >
            {t("resend")}
          </Button>
          <Button type="button" variant="ghost" size="sm" disabled={pending} onClick={() => setRevoking(true)} className="hover:bg-ink-50">
            {t("revoke")}
          </Button>
        </div>
      </div>

      {invitation.emailFailedAt && !resent && (
        <p className="rounded-xl bg-accent-100/60 px-3 py-2 text-xs text-accent-700">
          {t("emailFailed", { date: formatDate(invitation.emailFailedAt, locale) })}
        </p>
      )}
      {resent && !error && <p className="text-xs text-brand-700">{t("resent")}</p>}
      {error && !revoking && (
        <p role="alert" className="text-sm text-accent-700">
          {error}
        </p>
      )}

      <ConfirmDialog
        open={revoking}
        danger
        title={t("revokeTitle", { email: invitation.email })}
        confirmLabel={t("revokeConfirm")}
        cancelLabel={t("cancel")}
        pending={pending}
        error={revoking ? error : null}
        onConfirm={() => run(() => revokeInvitation(agencyId, invitation.id), () => setRevoking(false))}
        onCancel={() => {
          setRevoking(false);
          setError(null);
        }}
      >
        {t("revokeBody")}
      </ConfirmDialog>
    </li>
  );
}
