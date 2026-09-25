import { describe, expect, it } from "vitest";
import { adminViewHref, parseAdminView } from "@/lib/messaging/adminView";

describe("admin messaging tabs", () => {
  it("shows the Resolved tab only when asked for, Active otherwise", () => {
    expect(parseAdminView("resolved")).toBe("resolved");
    expect(parseAdminView(undefined)).toBe("active");
    expect(parseAdminView("active")).toBe("active");
    expect(parseAdminView("anything")).toBe("active");
    expect(parseAdminView(["resolved", "active"])).toBe("active");
  });

  it("links to each tab", () => {
    expect(adminViewHref("active")).toBe("/admin/messaging");
    expect(adminViewHref("resolved")).toBe("/admin/messaging?view=resolved");
  });
});
