import { describe, expect, it } from "vitest";
import { robotsRules } from "./robots";

describe("robotsRules", () => {
  const robots = robotsRules("https://imova.md");
  const rules = Array.isArray(robots.rules) ? robots.rules[0] : robots.rules;

  it("keeps private pages out, in every language", () => {
    expect(rules.disallow).toContain("/account");
    expect(rules.disallow).toContain("/ru/account");
    expect(rules.disallow).toContain("/en/messages");
    expect(rules.disallow).toContain("/ru/properties/new");
  });

  it("leaves the public pages open and points at the sitemap", () => {
    expect(rules.allow).toBe("/");
    for (const path of ["/search", "/property", "/agencies", "/map", "/ru/search", "/terms", "/privacy", "/en/privacy"]) {
      expect(rules.disallow).not.toContain(path);
    }
    expect(robots.sitemap).toBe("https://imova.md/sitemap.xml");
  });
});
