import { describe, expect, it } from "vitest";
import { parsePublicConfig, serializePublicConfig, serverPublicConfig } from "./publicConfig";

describe("serverPublicConfig", () => {
  it("prefers PUBLIC_API_URL and drops a trailing slash", () => {
    expect(serverPublicConfig({ PUBLIC_API_URL: "https://api.imova.md/", NEXT_PUBLIC_API_URL: "http://x" }))
      .toEqual({ apiUrl: "https://api.imova.md", posthogKey: null });
  });

  it("falls back to NEXT_PUBLIC_API_URL, then localhost", () => {
    expect(serverPublicConfig({ NEXT_PUBLIC_API_URL: "http://localhost:5000" })).toEqual({ apiUrl: "http://localhost:5000", posthogKey: null });
    expect(serverPublicConfig({})).toEqual({ apiUrl: "http://localhost:8080", posthogKey: null });
    expect(serverPublicConfig({ PUBLIC_API_URL: "" })).toEqual({ apiUrl: "http://localhost:8080", posthogKey: null });
  });

  it("reads the PostHog key, blank meaning off", () => {
    expect(serverPublicConfig({ POSTHOG_KEY: " phc_abc " }).posthogKey).toBe("phc_abc");
    expect(serverPublicConfig({ POSTHOG_KEY: "  " }).posthogKey).toBeNull();
  });
});

describe("serializePublicConfig / parsePublicConfig", () => {
  it("round-trips", () => {
    const config = { apiUrl: "https://imova-api-prod.example.azurecontainerapps.io", posthogKey: "phc_abc" };
    expect(parsePublicConfig(serializePublicConfig(config))).toEqual(config);
    expect(parsePublicConfig('{"apiUrl":"https://api.imova.md","posthogKey":5}')).toEqual({ apiUrl: "https://api.imova.md", posthogKey: null });
  });

  it("can't close the script element", () => {
    const text = serializePublicConfig({ apiUrl: "</script><script>alert(1)</script>", posthogKey: null });
    expect(text).not.toContain("<");
    expect(parsePublicConfig(text).apiUrl).toBe("</script><script>alert(1)</script>");
  });

  it("falls back to localhost on missing or malformed input", () => {
    for (const text of [null, undefined, "", "{", "[]", '{"apiUrl":5}', '{"apiUrl":""}']) {
      expect(parsePublicConfig(text)).toEqual({ apiUrl: "http://localhost:8080", posthogKey: null });
    }
  });
});
