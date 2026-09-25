import { notFound, redirect } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { MessagingAdminActions } from "@/components/admin/MessagingAdminActions";
import { MessageBubble } from "@/components/messaging/MessageBubble";
import { LinkButton } from "@/components/ui/Button";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getAdminConversation } from "@/lib/messaging/api";

// Read-only view of a whole conversation for reviewing a report (the initiator's messages on the
// left, the publisher's on the right).
export default async function AdminConversationPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const profile = await getCurrentUserProfile();
  if (!profile) {
    redirect(`/login?next=${encodeURIComponent(`/admin/messaging/conversations/${id}`)}`);
  }
  if (!profile.roles.includes("Admin")) {
    notFound();
  }

  const [t, tMessages, locale, conversation] = await Promise.all([
    getTranslations("AdminMessaging"),
    getTranslations("Messages"),
    getLocale(),
    getAdminConversation(id),
  ]);
  if (!conversation) {
    notFound();
  }

  const time = new Intl.DateTimeFormat(locale, { dateStyle: "short", timeStyle: "short" });
  const participants = [
    { label: t("initiator"), user: conversation.initiator },
    { label: t("publisher"), user: conversation.publisher },
  ];

  return (
    <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <LinkButton href="/admin/messaging" variant="ghost" size="sm" className="!px-0 !justify-start">
        ← {t("title")}
      </LinkButton>
      <h1 className="mt-3 font-hero text-2xl font-extrabold text-ink-950">{conversation.listing.title ?? tMessages("listingDeleted")}</h1>
      <ul className="mt-4 space-y-2">
        {participants.map(({ label, user }) => (
          <li key={user.id} className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-ink-100 bg-white px-4 py-3 text-sm">
            <span>
              <span className="font-medium text-ink-900">{label}:</span> {user.displayName ?? "—"} {user.email && `(${user.email})`}
            </span>
            <MessagingAdminActions user={user} />
          </li>
        ))}
      </ul>
      <div className="mt-6 space-y-2 rounded-2xl border border-ink-100 bg-ink-50/60 p-4">
        {conversation.messages.map((m) => (
          <MessageBubble
            key={m.id}
            message={m}
            mine={m.senderUserId === conversation.publisher.id}
            time={time.format(new Date(m.createdAt))}
            statusLabel={tMessages(`status.${m.status}`)}
          />
        ))}
      </div>
    </main>
  );
}
