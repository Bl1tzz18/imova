import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { InboxList } from "@/components/messaging/InboxList";
import { ThreadView } from "@/components/messaging/ThreadView";
import { TextInput } from "@/components/ui/Field";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getConversations, getThread } from "@/lib/messaging/api";
import { cn } from "@/lib/utils/cn";

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

  // The sidebar shows the list the open conversation belongs to: the archive for an archived one
  // (it wouldn't be in the inbox list), the inbox otherwise.
  const archived = thread.conversation.isArchived;
  const conversations = await getConversations(undefined, archived);
  const listHref = archived ? "/messages?archived=1" : "/messages";
  const tab = (active: boolean) =>
    cn(
      "flex-1 rounded-full px-3 py-1.5 text-center text-sm font-medium transition-colors",
      active ? "bg-ink-950 text-white" : "text-ink-600 hover:text-ink-950",
    );

  // Split view: the conversation list on the side (lg and up) and the open conversation filling the
  // rest of the screen below the header. On smaller screens only the conversation shows, with a
  // back arrow to that list. 81px = the sticky header (h-20 + its 1px bottom border), so the page
  // itself never scrolls.
  return (
    <main className="flex h-[calc(100dvh-81px)] bg-white">
      <aside className="hidden w-[380px] shrink-0 flex-col border-r border-line lg:flex">
        <div className="border-b border-line px-4 pb-4 pt-5">
          <h1 className="font-hero text-xl font-extrabold text-ink-950">{archived ? t("archive") : t("title")}</h1>
          <div className="mt-3 flex gap-1 rounded-full border border-line bg-white p-1">
            <Link href="/messages" aria-current={archived ? undefined : "page"} className={tab(!archived)}>
              {t("inbox")}
            </Link>
            <Link href="/messages?archived=1" aria-current={archived ? "page" : undefined} className={tab(archived)}>
              {t("archive")}
            </Link>
          </div>
          <form action="/messages" className="mt-3">
            {archived && <input type="hidden" name="archived" value="1" />}
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
            <p className="px-4 py-8 text-center text-sm text-ink-500">{archived ? t("emptyArchive") : t("empty")}</p>
          )}
        </div>
      </aside>

      <section className="min-w-0 flex-1">
        {/* Keyed so navigating to another conversation starts from its own state. */}
        <ThreadView key={thread.conversation.id} thread={thread} currentUserId={profile.id} backHref={listHref} />
      </section>
    </main>
  );
}
