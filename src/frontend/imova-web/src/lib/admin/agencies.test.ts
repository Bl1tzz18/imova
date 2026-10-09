import { describe, expect, it } from "vitest";
import { adminAgenciesApiQuery, adminAgenciesHref, parseAdminAgenciesParams } from "./agencies";

describe("/admin/agencies", () => {
  it("reads the address, falling back to every agency on page 1", () => {
    expect(parseAdminAgenciesParams({ q: " casa ", show: "unverified", page: "2" })).toEqual({ q: "casa", show: "unverified", page: 2 });
    expect(parseAdminAgenciesParams({ show: "pending", page: "0" })).toEqual({ q: "", show: "all", page: 1 });
  });

  it("writes only what differs from the defaults", () => {
    expect(adminAgenciesHref({ q: "", show: "all", page: 1 })).toBe("/admin/agencies");
    expect(adminAgenciesHref({ q: "imobil", show: "verified", page: 3 })).toBe("/admin/agencies?q=imobil&show=verified&page=3");
  });

  it("asks the API for the filter as verified=true/false, or nothing for all", () => {
    expect(adminAgenciesApiQuery({ q: "", show: "all", page: 1 })).toBe("page=1&pageSize=20");
    expect(adminAgenciesApiQuery({ q: "casa", show: "unverified", page: 2 })).toBe("q=casa&verified=false&page=2&pageSize=20");
    expect(adminAgenciesApiQuery({ q: "", show: "verified", page: 1 })).toBe("verified=true&page=1&pageSize=20");
  });
});
