import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { StartConversationForm } from "@/components/messaging/StartConversationForm";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getSessionToken } from "@/lib/auth/session";
import { getConversationIdForListing } from "@/lib/messaging/api";
import type { Listing } from "@/types/listing";

async function getListing(id: string): Promise<Listing | null> {
  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const token = await getSessionToken();
  const res = await fetch(`${apiUrl}/api/v1/listings/${id}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    cache: "no-store",
  });
  return res.ok ? res.json() : null;
}

// "Scrie mesaj": the first message about a listing. Anyone logged in except the listing's owner;
// an existing conversation about it opens instead.
export default async function NewConversationPage({ searchParams }: { searchParams: Promise<{ listing?: string }> }) {
  const { listing: listingId } = await searchParams;
  if (!listingId) {
    notFound();
  }

  const profile = await getCurrentUserProfile();
  if (!profile) {
    redirect(`/login?next=${encodeURIComponent(`/messages/new?listing=${listingId}`)}`);
  }

  const existing = await getConversationIdForListing(listingId);
  if (existing) {
    redirect(`/messages/${existing}`);
  }

  const [t, listing] = await Promise.all([getTranslations("Messages"), getListing(listingId)]);
  if (!listing || listing.status !== "Active") {
    notFound();
  }
  if (listing.publisher.userId === profile.id) {
    redirect(`/property/${listing.id}`);
  }

  const photo = listing.photos.find((p) => p.isPrimary) ?? listing.photos[0];

  return (
    <main className="mx-auto max-w-2xl px-4 py-10 sm:px-6">
      <h1 className="font-hero text-2xl font-extrabold text-ink-950">{t("newTitle")}</h1>

      <Link
        href={`/property/${listing.id}`}
        className="mt-6 flex items-center gap-4 rounded-2xl border border-ink-100 bg-white p-4 transition-colors hover:bg-ink-50"
      >
        <div className="h-16 w-20 shrink-0 overflow-hidden rounded-xl bg-ink-100">
          {photo && (
            // eslint-disable-next-line @next/next/no-img-element -- blob storage URL
            <img src={photo.url} alt="" className="h-full w-full object-cover" />
          )}
        </div>
        <div className="min-w-0">
          <p className="truncate font-semibold text-ink-950">{listing.title}</p>
          <p className="text-sm text-ink-500">{t("toPublisher", { name: listing.publisher.displayName })}</p>
        </div>
      </Link>

      <div className="mt-5">
        <StartConversationForm listingId={listing.id} />
      </div>
      <p className="mt-3 text-xs text-ink-500">{t("safetyTip")}</p>
    </main>
  );
}
