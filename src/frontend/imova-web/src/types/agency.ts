export type AgencyRole = "Owner" | "Admin" | "Agent";

export type InvitationStatus = "Pending" | "Accepted" | "Declined" | "Revoked" | "Expired";

// An invitation as its recipient sees it (GET /api/v1/invitations/{token}).
export type Invitation = {
  id: string;
  agencyId: string;
  agencyName: string;
  agencySlug: string;
  agencyLogoUrl: string | null;
  agencyIsVerified: boolean;
  // False while the agency is deactivated: it can still be joined, but isn't public for now.
  agencyIsActive: boolean;
  role: AgencyRole;
  invitedByName: string | null;
  email: string;
  status: InvitationStatus;
  expiresAt: string;
};
