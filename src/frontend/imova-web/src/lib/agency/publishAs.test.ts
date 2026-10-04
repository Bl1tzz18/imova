import { describe, expect, it } from "vitest";
import type { MyAgency } from "@/types/agency";
import { initialPublishAs, listingEditRights, publishAsOptions } from "./publishAs";

function agency(id: string, status: MyAgency["status"] = "Active"): MyAgency {
  return { id, name: `Agenția ${id}`, slug: id, logoThumbnailUrl: null, isVerified: false, status, role: "Agent" };
}

describe("publishAsOptions", () => {
  it("offers the active agencies only", () => {
    expect(publishAsOptions([agency("a"), agency("b", "Deactivated")]).map((o) => o.id)).toEqual(["a"]);
  });

  it("is empty for someone without an agency (no picker)", () => {
    expect(publishAsOptions([])).toEqual([]);
  });

  it("keeps the listing's current agency first when editing, even a deactivated one", () => {
    const current = { id: "b", name: "Agenția b", logoUrl: null, isVerified: false };
    expect(publishAsOptions([agency("a"), agency("b", "Deactivated")], current).map((o) => o.id)).toEqual(["b", "a"]);
  });
});

describe("initialPublishAs", () => {
  const options = publishAsOptions([agency("a"), agency("b")]);

  it("starts where the listing is when editing, whatever was remembered", () => {
    expect(initialPublishAs({ options, editingAgencyId: "b", remembered: "a" })).toBe("b");
    expect(initialPublishAs({ options, editingAgencyId: null, remembered: "a" })).toBe("");
  });

  it("prefers ?agencyId= when it's one of the user's agencies", () => {
    expect(initialPublishAs({ options, requested: "b", remembered: "a" })).toBe("b");
  });

  it("ignores an ?agencyId= that isn't theirs, falling back to the remembered choice", () => {
    expect(initialPublishAs({ options, requested: "zzz", remembered: "a" })).toBe("a");
  });

  it("remembers a private choice too", () => {
    expect(initialPublishAs({ options, remembered: "" })).toBe("");
  });

  it("drops a remembered agency they no longer belong to", () => {
    expect(initialPublishAs({ options, remembered: "gone" })).toBe("");
  });

  it("is private with nothing to go on", () => {
    expect(initialPublishAs({ options })).toBe("");
  });
});

describe("listingEditRights", () => {
  const base = { userId: "me", isSiteAdmin: false, authorUserId: "me", listingAgencyId: null as string | null, agencies: [] as MyAgency[] };
  const role = (r: MyAgency["role"]): MyAgency => ({ ...agency("a"), role: r });

  it("lets the author of a private listing edit it and put it under an agency", () => {
    expect(listingEditRights(base)).toEqual({ canEdit: true, canChangeAgency: true, isAuthor: true });
  });

  it("lets the agency's Owner or Admin edit an agent's listing and take it out of the agency (only private offered)", () => {
    for (const r of ["Owner", "Admin"] as const) {
      expect(listingEditRights({ ...base, authorUserId: "agent", listingAgencyId: "a", agencies: [role(r)] })).toEqual({
        canEdit: true,
        canChangeAgency: true,
        isAuthor: false,
      });
    }
  });

  it("doesn't let an Agent take their own agency listing out of the agency", () => {
    expect(listingEditRights({ ...base, listingAgencyId: "a", agencies: [role("Agent")] })).toEqual({
      canEdit: true,
      canChangeAgency: false,
      isAuthor: true,
    });
  });

  it("doesn't let another agent of the agency edit it", () => {
    expect(listingEditRights({ ...base, authorUserId: "agent", listingAgencyId: "a", agencies: [role("Agent")] }).canEdit).toBe(false);
  });

  it("lets a site admin edit anything", () => {
    expect(listingEditRights({ ...base, authorUserId: "someone", isSiteAdmin: true }).canEdit).toBe(true);
  });
});
