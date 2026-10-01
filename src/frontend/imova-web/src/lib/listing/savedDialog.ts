// What the "saved" dialog of the edit page tells the owner — decided by the listing's status
// *after* the save, so it never claims more than happened (e.g. "resubmitted" when the owner's
// email isn't confirmed and the listing stayed Rejected):
//   live        — Active: visitors already see the changes
//   resubmitted — was Rejected, now back in review
//   inReview    — was already waiting for a moderator; the changes are part of that review
//   needsEmail  — Draft: it goes to review by itself once the email is confirmed (see
//               ReviewEligibility in the API)
//   needsEmailToResubmit — still Rejected (email not confirmed): confirm, then save again
//   notPublic   — Suspended, Expired, Archived, Sold, Rented: saved, but nobody sees it now
export type SavedOutcome = "live" | "resubmitted" | "inReview" | "needsEmail" | "needsEmailToResubmit" | "notPublic";

export function savedOutcome(status: string, wasRejected: boolean): SavedOutcome {
  switch (status) {
    case "Active":
      return "live";
    case "PendingReview":
      return wasRejected ? "resubmitted" : "inReview";
    case "Draft":
      return "needsEmail";
    case "Rejected":
      return "needsEmailToResubmit";
    default:
      return "notPublic";
  }
}
