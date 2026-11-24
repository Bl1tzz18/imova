import { describe, expect, it } from "vitest";
import { activeTab, showsHeaderSearch, showsTabBar } from "./tabBar";

describe("showsTabBar", () => {
  it.each(["/", "/search", "/map", "/messages", "/saved-listings", "/account", "/my-listings", "/agencies/casa-ta"])(
    "shows on %s",
    (path) => expect(showsTabBar(path)).toBe(true),
  );

  it.each(["/property/100015-apartament", "/properties/new", "/my-listings/abc/edit", "/messages/abc", "/messages/new"])(
    "leaves the bottom to the page's own bar on %s",
    (path) => expect(showsTabBar(path)).toBe(false),
  );

  it("ignores a trailing slash and the query string", () => {
    expect(showsTabBar("/properties/new/")).toBe(false);
    expect(showsTabBar("/messages?archived=1")).toBe(true);
  });
});

describe("activeTab", () => {
  it.each([
    ["/", "home"],
    ["/messages", "messages"],
    ["/properties/new", "add"],
    ["/saved-listings", "saved"],
    ["/account", "account"],
    ["/account/agencies/new", "account"],
    ["/my-listings", "account"],
    ["/saved-searches", "account"],
  ])("%s → %s", (path, tab) => expect(activeTab(path)).toBe(tab));

  it("is none for pages outside the tabs", () => {
    expect(activeTab("/search")).toBeNull();
    expect(activeTab("/accounting")).toBeNull();
  });
});

describe("showsHeaderSearch", () => {
  it("hides on the search and map pages, which have their own field", () => {
    expect(showsHeaderSearch("/search")).toBe(false);
    expect(showsHeaderSearch("/map")).toBe(false);
    expect(showsHeaderSearch("/")).toBe(true);
    expect(showsHeaderSearch("/property/100015-x")).toBe(true);
  });
});
