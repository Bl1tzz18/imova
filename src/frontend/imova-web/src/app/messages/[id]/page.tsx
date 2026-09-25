import { notFound, redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { LinkButton } from "@/components/ui/Button";
import { ThreadView } from "@/components/messaging/ThreadView";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getThread } from "@/lib/messaging/api";

export default async function ConversationPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const profile = await getCurrentUserProfile();
  if (!profile) {
    redirect(`/login?next=${encodeURIComponent(`/messages/${id}`)}`);
  }

  const [t, thread] = await Promise.all([getTranslations("Messages"), getThread(id)]);
  if (!thread) {
    notFound();
  }

  return (
    <main className="mx-auto max-w-3xl px-4 py-6 sm:px-6">
      <LinkButton href="/messages" variant="ghost" size="sm" className="!px-0 !justify-start">
        ← {t("backToInbox")}
      </LinkButton>
      <div className="mt-3">
        {/* Keyed so navigating to another conversation starts from its own state. */}
        <ThreadView key={thread.conversation.id} thread={thread} currentUserId={profile.id} />
      </div>
    </main>
  );
}
