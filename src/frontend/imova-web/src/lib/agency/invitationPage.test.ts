import { describe, expect, it } from "vitest";
import type { Invitation, InvitationStatus } from "@/types/agency";
import { invitationPath, invitationView, sameEmail } from "./invitationPage";

function invitation(status: InvitationStatus = "Pending", email = "ana@example.com"): Invitation {
  return {
    id: "i1",
    agencyId: "a1",
    agencyName: "Casa Ta",
    agencySlug: "casa-ta",
    agencyLogoUrl: null,
    agencyIsVerified: false,
    agencyIsActive: true,
    role: "Agent",
    invitedByName: "Ion",
    email,
    status,
    expiresAt: "2026-10-11T12:00:00Z",
  };
}

describe("invitationView", () => {
  it("is notFound without an invitation", () => {
    expect(invitationView(null, null)).toBe("notFound");
    expect(invitationView(null, "ana@example.com")).toBe("notFound");
  });

  it("says expired, whoever is signed in", () => {
    expect(invitationView(invitation("Expired"), null)).toBe("expired");
    expect(invitationView(invitation("Expired"), "ana@example.com")).toBe("expired");
  });

  it.each<InvitationStatus>(["Accepted", "Declined", "Revoked"])("is closed once %s", (status) => {
    expect(invitationView(invitation(status), "ana@example.com")).toBe("closed");
  });

  it("asks to sign in when nobody is", () => {
    expect(invitationView(invitation(), null)).toBe("signIn");
  });

  it("is ready for the invited account, ignoring case", () => {
    expect(invitationView(invitation(), " Ana@Example.COM ")).toBe("ready");
  });

  it("offers to switch accounts for anyone else", () => {
    expect(invitationView(invitation(), "ion@example.com")).toBe("wrongAccount");
  });
});

describe("helpers", () => {
  it("compares addresses like the API", () => {
    expect(sameEmail("A@B.md", "a@b.md ")).toBe(true);
    expect(sameEmail("a@b.md", "a@c.md")).toBe(false);
  });

  it("builds the path to come back to", () => {
    expect(invitationPath("abc_DEF-1")).toBe("/invitations/abc_DEF-1");
  });
});
