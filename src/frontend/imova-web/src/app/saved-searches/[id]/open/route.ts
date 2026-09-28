import { redirect } from "next/navigation";
import { markSavedSearchViewed } from "@/lib/savedSearches/api";
import { SAVED_SEARCHES_PATH } from "@/lib/savedSearches/savedSearch";
import { SEARCH_PATH } from "@/lib/search/filters";

// Opening a saved search (from /saved-searches or an alert email): marks it viewed — its "N new"
// count starts over — then shows its results. Signed out → sign in and come back here.
// Redirects with a relative path: behind docker/a proxy the request's own origin is the server's
// bind address (0.0.0.0:3000), not the host the visitor used.
export async function GET(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const result = await markSavedSearchViewed(id);

  if (result === "signedOut") {
    redirect(`/login?next=${encodeURIComponent(`${SAVED_SEARCHES_PATH}/${id}/open`)}`);
  }
  if (result === "notFound") {
    redirect(SAVED_SEARCHES_PATH);
  }
  redirect(result.queryString ? `${SEARCH_PATH}?${result.queryString}` : SEARCH_PATH);
}
