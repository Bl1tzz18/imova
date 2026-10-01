"use client";

import { useEffect, useRef } from "react";
import { recordListingView } from "@/lib/listing/contactActions";

// Tells the API the listing was opened — once per page load, from the browser, so link prefetches
// and crawlers that don't run scripts aren't counted. Not rendered for the owner. Renders nothing.
export function ListingViewTracker({ listingId }: { listingId: string }) {
  const sent = useRef<string | null>(null);

  useEffect(() => {
    if (sent.current === listingId) return;
    sent.current = listingId;
    void recordListingView(listingId);
  }, [listingId]);

  return null;
}
