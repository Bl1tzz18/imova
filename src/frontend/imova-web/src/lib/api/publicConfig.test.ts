import { describe, expect, it } from "vitest";
import { parsePublicConfig, serializePublicConfig, serverPublicConfig } from "./publicConfig";

describe("serverPublicConfig", () => {
  it("prefers PUBLIC_API_URL and drops a trailing slash", () => {
    expect(serverPublicConfig({ PUBLIC_API_URL: "https://api.imova.md/", NEXT_PUBLIC_API_URL: "http://x" }))
      .toEqual({ apiUrl: "https://api.imova.md" });
  });

  it("falls back to NEXT_PUBLIC_API_URL, then localhost", () => {
    expect(serverPublicConfig({ NEXT_PUBLIC_API_URL: "http://localhost:5000" })).toEqual({ apiUrl: "http://localhost:5000" });
    expect(serverPublicConfig({})).toEqual({ apiUrl: "http://localhost:8080" });
    expect(serverPublicConfig({ PUBLIC_API_URL: "" })).toEqual({ apiUrl: "http://localhost:8080" });
  });
});

describe("serializePublicConfig / parsePublicConfig", () => {
  it("round-trips", () => {
    const config = { apiUrl: "https://imova-api-prod.example.azurecontainerapps.io" };
    expect(parsePublicConfig(serializePublicConfig(config))).toEqual(config);
  });

  it("can't close the script element", () => {
    const text = serializePublicConfig({ apiUrl: "</script><script>alert(1)</script>" });
    expect(text).not.toContain("<");
    expect(parsePublicConfig(text).apiUrl).toBe("</script><script>alert(1)</script>");
  });

  it("falls back to localhost on missing or malformed input", () => {
    for (const text of [null, undefined, "", "{", "[]", '{"apiUrl":5}', '{"apiUrl":""}']) {
      expect(parsePublicConfig(text)).toEqual({ apiUrl: "http://localhost:8080" });
    }
  });
});
