import Link from "next/link";
import { redirect } from "next/navigation";
import { getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { InboxList } from "@/components/messaging/InboxList";
import { TextInput } from "@/components/ui/Field";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getConversations } from "@/lib/messaging/api";
import { cn } from "@/lib/utils/cn";

export default async function MessagesPage({ searchParams }: { searchParams: Promise<{ q?: string; archived?: string }> }) {
  const { q, archived } = await searchParams;
  const showArchived = archived === "1";
  const profile = await getCurrentUserProfile();
  if (!profile) {
    redirect("/login?next=/messages");
  }

  const [t, conversations] = await Promise.all([getTranslations("Messages"), getConversations(q, showArchived)]);
  const tab = (active: boolean) =>
    cn("rounded-full px-4 py-1.5 text-sm font-medium transition-colors", active ? "bg-ink-950 text-white" : "text-ink-600 hover:bg-white");

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <div className="mx-auto max-w-3xl px-4 py-10 sm:px-6">
          <h1 className="font-hero text-3xl font-extrabold text-ink-950">{t("title")}</h1>

          <div className="mt-6 flex flex-wrap items-center justify-between gap-3">
            <div className="flex gap-1 rounded-full border border-ink-100 bg-ink-50 p-1">
              <Link href={q ? `/messages?q=${encodeURIComponent(q)}` : "/messages"} className={tab(!showArchived)}>
                {t("inbox")}
              </Link>
              <Link href={`/messages?archived=1${q ? `&q=${encodeURIComponent(q)}` : ""}`} className={tab(showArchived)}>
                {t("archive")}
              </Link>
            </div>
            <form className="w-full sm:w-72" action="/messages">
              {showArchived && <input type="hidden" name="archived" value="1" />}
              <TextInput name="q" defaultValue={q} placeholder={t("searchPlaceholder")} aria-label={t("searchPlaceholder")} />
            </form>
          </div>

          <div className="mt-5">
            {conversations && conversations.length > 0 ? (
              <InboxList conversations={conversations} currentUserId={profile.id} />
            ) : (
              <p className="rounded-2xl border border-dashed border-ink-200 px-6 py-12 text-center text-sm text-ink-500">
                {q ? t("noResults") : showArchived ? t("emptyArchive") : t("empty")}
              </p>
            )}
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
}
