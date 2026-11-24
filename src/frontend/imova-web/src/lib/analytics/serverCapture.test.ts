import { describe, expect, it } from "vitest";
import { serverCapturePayload } from "./serverCapture";

const KEY = "phc_test";
const phCookie = (value: object) => `ph_${KEY}_posthog=${encodeURIComponent(JSON.stringify(value))}`;

describe("serverCapturePayload", () => {
  it("sends the event under the browser's PostHog id when analytics were accepted", () => {
    const header = `imova_consent=1%3Aanalytics; ${phCookie({ distinct_id: "abc-123", $sesid: [1, "s", 1] })}`;

    expect(serverCapturePayload(header, KEY, "account_created", { method: "google" })).toEqual({
      api_key: KEY,
      event: "account_created",
      distinct_id: "abc-123",
      properties: { method: "google" },
    });
  });

  it("sends nothing without consent, after a refusal or for an outdated answer", () => {
    const ph = phCookie({ distinct_id: "abc-123" });

    expect(serverCapturePayload(ph, KEY, "account_created", {})).toBeNull();
    expect(serverCapturePayload(`imova_consent=1%3A; ${ph}`, KEY, "account_created", {})).toBeNull();
    expect(serverCapturePayload(`imova_consent=0%3Aanalytics; ${ph}`, KEY, "account_created", {})).toBeNull();
  });

  it("sends nothing when PostHog hasn't given this browser an id", () => {
    const consent = "imova_consent=1%3Aanalytics";

    expect(serverCapturePayload(consent, KEY, "account_created", {})).toBeNull();
    expect(serverCapturePayload(`${consent}; ph_other_posthog=${encodeURIComponent('{"distinct_id":"x"}')}`, KEY, "account_created", {})).toBeNull();
    expect(serverCapturePayload(`${consent}; ph_${KEY}_posthog=not-json`, KEY, "account_created", {})).toBeNull();
    expect(serverCapturePayload(`${consent}; ${phCookie({ distinct_id: "" })}`, KEY, "account_created", {})).toBeNull();
  });
});
