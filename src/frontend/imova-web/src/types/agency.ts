export type AgencyRole = "Owner" | "Admin" | "Agent";

export type InvitationStatus = "Pending" | "Accepted" | "Declined" | "Revoked" | "Expired";

// An agency's profile (GET /api/v1/agencies/by-slug/{slug}). `phone` only for its members and admins;
// everyone else gets its shape (phonePrefix + phoneHiddenDigits) and asks for the number itself.
// logoUrl: 512 px square, logoThumbnailUrl: 128 px. myRole: the caller's role, null for a non-member.
export type Agency = {
  id: string;
  slug: string;
  name: string;
  logoUrl: string | null;
  logoThumbnailUrl: string | null;
  bio: string | null;
  phone: string | null;
  phonePrefix: string | null;
  phoneHiddenDigits: number | null;
  email: string;
  website: string | null;
  address: string | null;
  raionId: string | null;
  raionName: string | null;
  isVerified: boolean;
  verifiedAt: string | null;
  status: "Active" | "Deactivated";
  createdAt: string;
  memberCount: number;
  activeListingCount: number;
  myRole: AgencyRole | null;
};

// An agency in the public directory (GET /api/v1/agencies).
export type AgencyCard = {
  id: string;
  slug: string;
  name: string;
  logoThumbnailUrl: string | null;
  isVerified: boolean;
  raionName: string | null;
  activeListingCount: number;
};

export type AgencyDirectoryPage = { items: AgencyCard[]; page: number; pageSize: number; totalCount: number };

// An agency the signed-in user belongs to (GET /api/v1/users/me/agencies). logoThumbnailUrl: 128 px.
export type MyAgency = {
  id: string;
  name: string;
  slug: string;
  logoThumbnailUrl: string | null;
  isVerified: boolean;
  status: "Active" | "Deactivated";
  role: AgencyRole;
};

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
