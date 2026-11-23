import { describe, expect, it } from "vitest";
import { wwwRedirect } from "./canonicalHost";

describe("wwwRedirect", () => {
  const site = "https://imova.md";

  it("sends www to the same path and query on the site's address", () => {
    expect(wwwRedirect("www.imova.md", site, "/ru/search", "?propertyType=House")).toBe(
      "https://imova.md/ru/search?propertyType=House",
    );
    expect(wwwRedirect("WWW.Imova.md:443", site, "/", "")).toBe("https://imova.md/");
  });

  it("leaves every other host alone", () => {
    expect(wwwRedirect("imova.md", site, "/", "")).toBeNull();
    expect(wwwRedirect("imova-web.orangeflower.polandcentral.azurecontainerapps.io", site, "/", "")).toBeNull();
    expect(wwwRedirect("localhost:3000", "http://localhost:3000", "/", "")).toBeNull();
    expect(wwwRedirect(null, site, "/", "")).toBeNull();
  });

  it("does nothing while the site itself is on a www address", () => {
    expect(wwwRedirect("www.imova.md", "https://www.imova.md", "/", "")).toBeNull();
  });
});
