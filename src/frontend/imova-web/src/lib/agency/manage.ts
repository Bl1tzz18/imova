import type { Agency, AgencyMember, AgencyRole } from "@/types/agency";

// The agency management pages (/account/agencies/...) — pure rules, Vitest-covered. Who may do what
// mirrors the API's AgencyAccess (which has the last word); the UI only leaves out what would be refused.

export const AGENCY_SECTIONS = ["profile", "members", "listings", "settings"] as const;
export type AgencySection = (typeof AGENCY_SECTIONS)[number];

// The "Agențiile mele" tab of /account.
export const MY_AGENCIES_HREF = "/account?tab=agencies";
export const NEW_AGENCY_HREF = "/account/agencies/new";

export function agencyManagePath(agencyId: string, section: AgencySection = "profile"): string {
  return section === "profile" ? `/account/agencies/${agencyId}` : `/account/agencies/${agencyId}/${section}`;
}

// Which section a pathname shows (the tab to mark), "profile" for the agency's own address.
export function sectionOf(pathname: string): AgencySection {
  const last = pathname.replace(/\/+$/, "").split("/").pop();
  return AGENCY_SECTIONS.find((s) => s === last) ?? "profile";
}

// Owner < Admin < Agent, as the API orders them.
const RANK: Record<AgencyRole, number> = { Owner: 0, Admin: 1, Agent: 2 };

// Owners and Admins edit the profile and logo, and manage members and every listing of the agency.
export function canManage(role: AgencyRole | null, isSiteAdmin = false): boolean {
  return isSiteAdmin || role === "Owner" || role === "Admin";
}

// The roles a new member may be invited with: Owners invite Admins and Agents, Admins only Agents
// (nobody is invited straight in as an Owner).
export function inviteRoles(actorRole: AgencyRole | null, isSiteAdmin = false): AgencyRole[] {
  if (isSiteAdmin || actorRole === "Owner") return ["Agent", "Admin"];
  return actorRole === "Admin" ? ["Agent"] : [];
}

function ownerCount(members: AgencyMember[]): number {
  return members.filter((m) => m.role === "Owner").length;
}

export function isLastOwner(members: AgencyMember[], userId: string): boolean {
  const member = members.find((m) => m.userId === userId);
  return member?.role === "Owner" && ownerCount(members) === 1;
}

// The roles `target` can be given by the actor, its current one included; empty when there's
// nothing to choose (show the role as text). Owners change anyone; anyone may step down themselves;
// an Admin can't change another member's role (Agents are already Agents). The last Owner keeps the
// role until someone else is made Owner.
export function roleChoices(
  actor: { userId: string; role: AgencyRole | null; isSiteAdmin?: boolean },
  target: AgencyMember,
  members: AgencyMember[],
): AgencyRole[] {
  let choices: AgencyRole[];
  if (actor.isSiteAdmin || actor.role === "Owner") {
    choices = ["Owner", "Admin", "Agent"];
  } else if (actor.userId === target.userId && actor.role) {
    const own = RANK[actor.role];
    choices = (["Owner", "Admin", "Agent"] as const).filter((r) => RANK[r] >= own);
  } else {
    choices = [target.role];
  }

  if (isLastOwner(members, target.userId)) {
    choices = ["Owner"];
  }
  return choices.length > 1 ? choices : [];
}

// Removing someone else: Owners remove anyone (but not the last Owner), Admins only Agents. Leaving
// is a separate action (canLeave).
export function canRemove(
  actor: { userId: string; role: AgencyRole | null; isSiteAdmin?: boolean },
  target: AgencyMember,
  members: AgencyMember[],
): boolean {
  if (actor.userId === target.userId || isLastOwner(members, target.userId)) return false;
  if (actor.isSiteAdmin || actor.role === "Owner") return true;
  return actor.role === "Admin" && target.role === "Agent";
}

// Anyone but the last Owner may leave.
export function canLeave(members: AgencyMember[], userId: string): boolean {
  return members.some((m) => m.userId === userId) && !isLastOwner(members, userId);
}

// Who can take over the listings of someone leaving: an Owner or Admin who stays — Owners first,
// then Admins, longest-standing first.
export function heirOptions(members: AgencyMember[], leavingUserId: string): AgencyMember[] {
  return members
    .filter((m) => m.userId !== leavingUserId && (m.role === "Owner" || m.role === "Admin"))
    .sort((a, b) => RANK[a.role] - RANK[b.role] || a.joinedAt.localeCompare(b.joinedAt));
}

// The heir the API picks when none is chosen, so the dialog starts on it: whoever removes them (an
// Owner/Admin who stays), else the longest-standing Owner, else Admin. Null when nobody qualifies.
export function defaultHeir(members: AgencyMember[], leavingUserId: string, actorId: string | null): string | null {
  const options = heirOptions(members, leavingUserId);
  if (actorId && options.some((m) => m.userId === actorId)) return actorId;
  return options[0]?.userId ?? null;
}

// --- The profile form ---

export const AGENCY_NAME_MAX = 120;
export const AGENCY_BIO_MAX = 2000;
export const AGENCY_ADDRESS_MAX = 250;
export const AGENCY_WEBSITE_MAX = 254;
export const AGENCY_EMAIL_MAX = 254;

export type AgencyProfileValues = {
  name: string;
  phone: string;
  email: string;
  bio: string;
  website: string;
  address: string;
  raionId: string;
};

export const EMPTY_AGENCY_PROFILE: AgencyProfileValues = {
  name: "",
  phone: "",
  email: "",
  bio: "",
  website: "",
  address: "",
  raionId: "",
};

export function agencyProfileValues(agency: Agency): AgencyProfileValues {
  return {
    name: agency.name,
    phone: agency.phone ?? "",
    email: agency.email,
    bio: agency.bio ?? "",
    website: agency.website ?? "",
    address: agency.address ?? "",
    raionId: agency.raionId ?? "",
  };
}

// A phone written two ways ("+373 22 555 010" / "+37322555010") is the same phone.
function comparable(field: keyof AgencyProfileValues, value: string): string {
  const trimmed = value.trim();
  return field === "phone" ? trimmed.replace(/[\s().-]/g, "") : trimmed;
}

// Whether the form differs from what's saved — Save stays off, and leaving doesn't warn, until it does.
export function agencyProfileChanged(saved: AgencyProfileValues, current: AgencyProfileValues): boolean {
  return (Object.keys(saved) as (keyof AgencyProfileValues)[]).some(
    (field) => comparable(field, saved[field]) !== comparable(field, current[field]),
  );
}

// The bio's character counter: its length, and whether it's nearly full or over.
export function bioCounter(bio: string): { length: number; max: number; nearLimit: boolean; over: boolean } {
  const length = bio.length;
  return { length, max: AGENCY_BIO_MAX, nearLimit: length >= AGENCY_BIO_MAX * 0.9, over: length > AGENCY_BIO_MAX };
}

// "CT" for "Casa Ta Imobiliare" — the logo placeholder (same rule as Avatar's initials).
export function agencyInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
  return (parts[0] ?? "?").slice(0, 2).toUpperCase();
}
