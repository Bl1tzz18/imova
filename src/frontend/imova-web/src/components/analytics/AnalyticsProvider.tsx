"use client";

import { useEffect, useState } from "react";
import type { PostHog } from "posthog-js";
import { usePathname } from "@/i18n/navigation";
import { readPublicConfig } from "@/lib/api/publicConfig";
import { attachAnalyticsClient } from "@/lib/analytics/events";
import { redactUrl, replayAllowed } from "@/lib/analytics/privacy";
import { CONSENT_CHANGED_EVENT, readConsent, type Consent } from "@/lib/consent/consent";

// Product analytics (PostHog, EU cloud), only when the environment has a POSTHOG_KEY
// (lib/api/publicConfig) and only after the visitor accepts analytics cookies: before an answer,
// and after "Refuz", PostHog isn't even loaded — nothing is sent anywhere (EDPB Guidelines 2/2023:
// consent covers cookieless tracking too). After "Accept": its own cookie, identify by account id
// (never email or name), and session replay with every input masked, never on the pages
// replayAllowed() rules out. Withdrawing consent later opts out and drops PostHog's cookie.
// Requests go through our own /ingest (next.config rewrites), so the browser only talks to imova.md.
export function AnalyticsProvider({ userId }: { userId: string | null }) {
  const [posthog, setPosthog] = useState<PostHog | null>(null);
  const [consented, setConsented] = useState(false);
  const pathname = usePathname();

  // The visitor's answer, now and whenever the banner changes it.
  useEffect(() => {
    if (!readPublicConfig().posthogKey) return;
    setConsented(readConsent()?.analytics === true);
    const onChange = (e: Event) => setConsented((e as CustomEvent<Consent>).detail.analytics);
    window.addEventListener(CONSENT_CHANGED_EVENT, onChange);
    return () => window.removeEventListener(CONSENT_CHANGED_EVENT, onChange);
  }, []);

  // Loaded on the first "Accept" (or a page load with consent already given), never before.
  useEffect(() => {
    if (!consented) return;
    if (posthog) {
      // Accepted again after a "Refuz" in the same visit.
      posthog.set_config({ disable_persistence: false });
      posthog.opt_in_capturing();
      return;
    }
    const key = readPublicConfig().posthogKey;
    if (!key) return;
    let cancelled = false;
    void import("posthog-js").then(({ default: ph }) => {
      if (cancelled) return;
      ph.init(key, {
        api_host: "/ingest",
        ui_host: "https://eu.posthog.com",
        defaults: "2026-01-30",
        person_profiles: "identified_only",
        capture_pageview: "history_change",
        capture_pageleave: true,
        capture_exceptions: true,
        disable_session_recording: true,
        session_recording: { maskAllInputs: true },
        before_send: (event) => {
          for (const prop of ["$current_url", "$referrer", "$initial_referrer"]) {
            const value = event.properties[prop];
            if (typeof value === "string") event.properties[prop] = redactUrl(value);
          }
          return event;
        },
      });
      // A consent withdrawn earlier left PostHog opted out in this browser — this is a new "Accept".
      ph.opt_in_capturing();
      attachAnalyticsClient(ph);
      setPosthog(ph);
    });
    return () => {
      cancelled = true;
    };
  }, [consented, posthog]);

  // "Refuz" after an "Accept" in the same visit: stop, keep nothing in memory-only mode, and delete
  // everything PostHog stored (its cookie, localStorage, its own opt-out marker).
  useEffect(() => {
    if (!posthog || consented) return;
    posthog.stopSessionRecording();
    posthog.opt_out_capturing();
    posthog.set_config({ disable_persistence: true });
    removePostHogStorage();
  }, [posthog, consented]);

  // Who is signed in — only with consent. reset() clears PostHog's own consent state too, so the
  // answer is applied again right after (reset first, then opt in).
  useEffect(() => {
    if (!posthog || !consented) return;
    // PostHog remembers (in its cookie) whether this browser was identified, so a sign-out is
    // noticed after a reload too.
    const wasIdentified = posthog.get_property("$user_state") === "identified";
    if (userId && posthog.get_distinct_id() !== userId) {
      posthog.identify(userId);
    } else if (!userId && wasIdentified) {
      posthog.reset();
      posthog.opt_in_capturing();
    }
  }, [posthog, consented, userId]);

  useEffect(() => {
    if (!posthog || !consented) return;
    if (replayAllowed(pathname)) posthog.startSessionRecording();
    else posthog.stopSessionRecording();
  }, [posthog, consented, pathname]);

  return null;
}

// Everything PostHog keeps in the browser: "ph_<key>_posthog" (cookie + localStorage), its
// session/opt-in markers ("__ph…"). Its cookie is set on the parent domain (.imova.md, shared by
// subdomains), so it's expired there as well as on the host itself.
function removePostHogStorage() {
  const isPostHog = (name: string) => name.startsWith("ph_") || name.startsWith("__ph");
  for (const storage of [localStorage, sessionStorage]) {
    try {
      for (const key of Object.keys(storage)) if (isPostHog(key)) storage.removeItem(key);
    } catch {
      // storage blocked: nothing was stored either
    }
  }
  const host = window.location.hostname;
  const domains = ["", `; Domain=${host}`, `; Domain=.${host.split(".").slice(-2).join(".")}`];
  for (const part of document.cookie.split(";")) {
    const name = part.trim().split("=")[0];
    if (!isPostHog(name)) continue;
    for (const domain of domains) document.cookie = `${name}=; Path=/; Max-Age=0${domain}`;
  }
}
