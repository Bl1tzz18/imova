import type { Listing } from "@/types/listing";

// Mirrors Imova.Contracts/Listings/ListingReportDtos.cs.
export type ListingReportReason = "Fraud" | "WrongInformation" | "NoLongerAvailable" | "Duplicate" | "Other";

export type ListingReportOutcome = "ListingSuspended" | "Dismissed";

export type ReportListingResult = { amended: boolean };

export type ListingReportSummary = { openListings: number; openReports: number };

export type ListingReporter = {
  userId: string;
  displayName: string | null;
  email: string | null;
  isDeleted: boolean;
  reportsFiled: number;
  reportsDismissed: number;
};

export type ListingReportItem = {
  id: string;
  reason: ListingReportReason;
  details: string | null;
  createdAt: string;
  updatedAt: string;
  reporter: ListingReporter;
};

export type ListingReportResolution = {
  outcome: ListingReportOutcome;
  resolvedAt: string;
  resolvedByUserId: string;
  resolvedByName: string | null;
  note: string | null;
};

export type ReportedListing = {
  listing: Listing;
  reportCount: number;
  reasons: { reason: ListingReportReason; count: number }[];
  firstReportedAt: string;
  lastReportedAt: string;
  reports: ListingReportItem[];
  resolution: ListingReportResolution | null;
};
