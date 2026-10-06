import type { Invitation } from "@/types/agency";

// What /invitations/[token] shows, from the invitation (null: the link doesn't match one) and who
// is signed in (null: nobody).
// - notFound: a mistyped or replaced link (resending an invitation makes a new link).
// - expired: ask the agency to send it again.
// - closed: already accepted, declined or revoked.
// - signIn: sign in (or create an account) with the invited address, then come back.
// - wrongAccount: signed in as someone else — switch accounts.
// - ready: Accept / Decline.
export type InvitationView = "notFound" | "expired" | "closed" | "signIn" | "wrongAccount" | "ready";

export function invitationView(invitation: Invitation | null, signedInEmail: string | null): InvitationView {
  if (!invitation) return "notFound";
  if (invitation.status === "Expired") return "expired";
  if (invitation.status !== "Pending") return "closed";
  if (signedInEmail === null) return "signIn";
  return sameEmail(signedInEmail, invitation.email) ? "ready" : "wrongAccount";
}

// Addresses compare ignoring case and surrounding spaces, as the API does.
export function sameEmail(a: string, b: string): boolean {
  return a.trim().toLowerCase() === b.trim().toLowerCase();
}

// Where sign-in / sign-up should come back to.
export function invitationPath(token: string): string {
  return `/invitations/${encodeURIComponent(token)}`;
}
