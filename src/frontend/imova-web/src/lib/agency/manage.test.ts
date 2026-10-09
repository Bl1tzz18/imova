import { describe, expect, it } from "vitest";
import {
  agencyListingsApiQuery,
  agencyListingsTabHref,
  parseAgencyListingsParams,
  tabOfGroup,
  agencyInitials,
  agencyManagePath,
  agencyProfileChanged,
  bioCounter,
  canDeleteAgency,
  canLeave,
  canManage,
  canRemove,
  defaultHeir,
  EMPTY_AGENCY_PROFILE,
  heirOptions,
  inviteRoles,
  roleChoices,
  sectionOf,
} from "./manage";
import type { AgencyMember, AgencyRole } from "@/types/agency";

function member(userId: string, role: AgencyRole, joinedAt = "2026-01-01T00:00:00Z"): AgencyMember {
  return { userId, name: userId, email: `${userId}@x.md`, pictureUrl: null, role, joinedAt, listingCount: 0 };
}

const owner = member("owner", "Owner", "2026-01-01T00:00:00Z");
const admin = member("admin", "Admin", "2026-02-01T00:00:00Z");
const agent = member("agent", "Agent", "2026-03-01T00:00:00Z");
const team = [owner, admin, agent];

describe("paths", () => {
  it("puts the profile at the agency's own address and the rest under it", () => {
    expect(agencyManagePath("a1")).toBe("/account/agencies/a1");
    expect(agencyManagePath("a1", "members")).toBe("/account/agencies/a1/members");
  });

  it("finds the section a pathname shows", () => {
    expect(sectionOf("/account/agencies/a1")).toBe("profile");
    expect(sectionOf("/account/agencies/a1/listings")).toBe("listings");
    expect(sectionOf("/account/agencies/a1/settings/")).toBe("settings");
  });
});

describe("who manages", () => {
  it("lets Owners, Admins and site admins manage, not Agents", () => {
    expect(canManage("Owner")).toBe(true);
    expect(canManage("Admin")).toBe(true);
    expect(canManage("Agent")).toBe(false);
    expect(canManage(null)).toBe(false);
    expect(canManage(null, true)).toBe(true);
  });

  it("lets only Owners and site admins delete the agency", () => {
    expect(canDeleteAgency("Owner")).toBe(true);
    expect(canDeleteAgency("Admin")).toBe(false);
    expect(canDeleteAgency("Agent")).toBe(false);
    expect(canDeleteAgency(null, true)).toBe(true);
  });

  it("lets Owners invite Admins and Agents, Admins only Agents, Agents nobody", () => {
    expect(inviteRoles("Owner")).toEqual(["Agent", "Admin"]);
    expect(inviteRoles("Admin")).toEqual(["Agent"]);
    expect(inviteRoles("Agent")).toEqual([]);
    expect(inviteRoles(null, true)).toEqual(["Agent", "Admin"]);
  });
});

describe("roleChoices", () => {
  it("gives an Owner every role for anyone", () => {
    expect(roleChoices({ userId: "owner", role: "Owner" }, agent, team)).toEqual(["Owner", "Admin", "Agent"]);
  });

  it("keeps the last Owner an Owner, until there's another", () => {
    expect(roleChoices({ userId: "owner", role: "Owner" }, owner, team)).toEqual([]);
    const second = member("owner2", "Owner");
    expect(roleChoices({ userId: "owner", role: "Owner" }, owner, [...team, second])).toEqual(["Owner", "Admin", "Agent"]);
  });

  it("lets an Admin step down but not change anyone else", () => {
    expect(roleChoices({ userId: "admin", role: "Admin" }, admin, team)).toEqual(["Admin", "Agent"]);
    expect(roleChoices({ userId: "admin", role: "Admin" }, agent, team)).toEqual([]);
    expect(roleChoices({ userId: "admin", role: "Admin" }, owner, team)).toEqual([]);
  });

  it("gives an Agent nothing to choose", () => {
    expect(roleChoices({ userId: "agent", role: "Agent" }, agent, team)).toEqual([]);
  });
});

describe("removing and leaving", () => {
  it("lets Owners remove anyone but themselves and the last Owner, Admins only Agents", () => {
    expect(canRemove({ userId: "owner", role: "Owner" }, admin, team)).toBe(true);
    expect(canRemove({ userId: "owner", role: "Owner" }, owner, team)).toBe(false);
    expect(canRemove({ userId: "admin", role: "Admin" }, agent, team)).toBe(true);
    expect(canRemove({ userId: "admin", role: "Admin" }, owner, team)).toBe(false);
    expect(canRemove({ userId: "agent", role: "Agent" }, admin, team)).toBe(false);
    expect(canRemove({ userId: "x", role: null, isSiteAdmin: true }, owner, team)).toBe(false);
    expect(canRemove({ userId: "x", role: null, isSiteAdmin: true }, agent, team)).toBe(true);
  });

  it("lets anyone but the last Owner leave", () => {
    expect(canLeave(team, "agent")).toBe(true);
    expect(canLeave(team, "owner")).toBe(false);
    expect(canLeave([...team, member("owner2", "Owner")], "owner")).toBe(true);
    expect(canLeave(team, "stranger")).toBe(false);
  });
});

describe("heirs", () => {
  const laterOwner = member("owner2", "Owner", "2026-04-01T00:00:00Z");

  it("offers the Owners and Admins who stay, Owners first, longest-standing first", () => {
    expect(heirOptions([agent, admin, laterOwner, owner], "agent").map((m) => m.userId)).toEqual(["owner", "owner2", "admin"]);
    expect(heirOptions(team, "admin").map((m) => m.userId)).toEqual(["owner"]);
  });

  it("starts on whoever removes them, else the longest-standing Owner", () => {
    expect(defaultHeir(team, "agent", "admin")).toBe("admin");
    expect(defaultHeir(team, "agent", "agent")).toBe("owner");
    expect(defaultHeir([...team, laterOwner], "agent", null)).toBe("owner");
  });

  it("has no heir when nobody qualifies", () => {
    expect(defaultHeir([owner, agent], "owner", "owner")).toBeNull();
  });
});

describe("the Anunțuri tab's address", () => {
  it("reads tab, search, sort and page, ignoring what it doesn't know", () => {
    expect(parseAgencyListingsParams({ tab: "ended", q: "  botanica ", sort: "priceAsc", page: "3" })).toEqual({
      tab: "ended",
      q: "botanica",
      sort: "priceAsc",
      page: 3,
    });
    expect(parseAgencyListingsParams({ tab: "all", sort: "cheap", page: "-1" })).toEqual({
      tab: undefined,
      q: "",
      sort: "recommended",
      page: 1,
    });
  });

  it("writes only what differs from the defaults", () => {
    expect(agencyListingsTabHref("a1", { sort: "recommended", page: 1 })).toBe("/account/agencies/a1/listings");
    expect(agencyListingsTabHref("a1", { tab: "unpublished", q: "garaj", sort: "newest", page: 2 })).toBe(
      "/account/agencies/a1/listings?tab=unpublished&q=garaj&sort=newest&page=2",
    );
  });

  it("asks the API for one page of 24, the tab only when one was chosen", () => {
    expect(agencyListingsApiQuery({ q: "", sort: "recommended", page: 1 })).toBe("sort=recommended&page=1&pageSize=24");
    expect(agencyListingsApiQuery({ tab: "ended", q: "casa", sort: "priceDesc", page: 4 })).toBe(
      "group=ended&q=casa&sort=priceDesc&page=4&pageSize=24",
    );
  });

  it("maps the API's group names to tabs", () => {
    expect(tabOfGroup("Unpublished")).toBe("unpublished");
    expect(tabOfGroup("Whatever")).toBe("active");
  });
});

describe("the profile form", () => {
  const saved = { ...EMPTY_AGENCY_PROFILE, name: "Casa Ta", phone: "+373 22 555 010", email: "o@casata.md" };

  it("sees no change in whitespace or in how the phone is written", () => {
    expect(agencyProfileChanged(saved, { ...saved, name: " Casa Ta ", phone: "+37322555010" })).toBe(false);
  });

  it("sees a real change", () => {
    expect(agencyProfileChanged(saved, { ...saved, bio: "Din 2010." })).toBe(true);
    expect(agencyProfileChanged(saved, { ...saved, phone: "+373 22 555 011" })).toBe(true);
  });

  it("counts the bio against its limit", () => {
    expect(bioCounter("abc")).toEqual({ length: 3, max: 2000, nearLimit: false, over: false });
    expect(bioCounter("a".repeat(1800)).nearLimit).toBe(true);
    expect(bioCounter("a".repeat(2001)).over).toBe(true);
  });

  it("makes initials for the logo placeholder", () => {
    expect(agencyInitials("Casa Ta Imobiliare")).toBe("CT");
    expect(agencyInitials("imobil")).toBe("IM");
    expect(agencyInitials("  ")).toBe("?");
  });
});
