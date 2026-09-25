import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { InboxList } from "@/components/messaging/InboxList";
import { ThreadView } from "@/components/messaging/ThreadView";
import { TextInput } from "@/components/ui/Field";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getConversations, getThread } from "@/lib/messaging/api";

export default async function ConversationPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const profile = await getCurrentUserProfile();
  if (!profile) {
    redirect(`/login?next=${encodeURIComponent(`/messages/${id}`)}`);
  }

  const [t, thread, conversations] = await Promise.all([getTranslations("Messages"), getThread(id), getConversations()]);
  if (!thread) {
    notFound();
  }

  // Split view: the conversation list on the side (lg and up) and the open conversation filling the
  // rest of the screen below the header. On smaller screens only the conversation shows, with a
  // back arrow to the inbox. 81px = the sticky header (h-20 + its 1px bottom border), so the page
  // itself never scrolls.
  return (
    <main className="flex h-[calc(100dvh-81px)] bg-white">
      <aside className="hidden w-[380px] shrink-0 flex-col border-r border-line lg:flex">
        <div className="border-b border-line px-4 pb-4 pt-5">
          <div className="flex items-center justify-between gap-3">
            <h1 className="font-hero text-xl font-extrabold text-ink-950">{t("title")}</h1>
            <Link href="/messages?archived=1" className="text-sm font-medium text-ink-500 transition-colors hover:text-ink-900">
              {t("archive")}
            </Link>
          </div>
          <form action="/messages" className="mt-3">
            <TextInput
              name="q"
              placeholder={t("searchPlaceholder")}
              aria-label={t("searchPlaceholder")}
              className="rounded-full border-line bg-bubble"
            />
          </form>
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto">
          {conversations && conversations.length > 0 ? (
            <InboxList conversations={conversations} currentUserId={profile.id} activeConversationId={thread.conversation.id} variant="sidebar" />
          ) : (
            <p className="px-4 py-8 text-center text-sm text-ink-500">{t("empty")}</p>
          )}
        </div>
      </aside>

      <section className="min-w-0 flex-1">
        {/* Keyed so navigating to another conversation starts from its own state. */}
        <ThreadView key={thread.conversation.id} thread={thread} currentUserId={profile.id} backHref="/messages" />
      </section>
    </main>
  );
}
