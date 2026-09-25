import Link from "next/link";
import { redirect } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { MessagingAdminActions } from "@/components/admin/MessagingAdminActions";
import { Badge } from "@/components/ui/Badge";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { getFlaggedMessages, getMessagingReports } from "@/lib/messaging/api";
import type { MessagingUser } from "@/types/messaging";

function who(user: MessagingUser) {
  return `${user.displayName ?? "—"}${user.email ? ` (${user.email})` : ""}`;
}

// Minimal moderation for messaging: open (or all) reports and messages the content filter flagged.
export default async function AdminMessagingPage({ searchParams }: { searchParams: Promise<{ all?: string }> }) {
  const { all } = await searchParams;
  const profile = await getCurrentUserProfile();
  if (!profile) {
    redirect("/login?next=/admin/messaging");
  }

  const [t, tMessages, locale] = await Promise.all([getTranslations("AdminMessaging"), getTranslations("Messages"), getLocale()]);
  if (!profile.roles.includes("Admin")) {
    return (
      <main className="mx-auto max-w-2xl px-4 py-16 text-center sm:px-6">
        <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">{t("forbidden")}</p>
      </main>
    );
  }

  const [reports, flagged] = await Promise.all([getMessagingReports(all === "1"), getFlaggedMessages()]);
  const date = new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "short" });

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <div className="mx-auto max-w-5xl px-4 py-10 sm:px-6">
          <h1 className="font-hero text-3xl font-extrabold text-ink-950">{t("title")}</h1>

          <section className="mt-8">
            <div className="flex items-center justify-between gap-3">
              <h2 className="font-display text-xl font-medium text-ink-950">{t("reports")}</h2>
              <Link href={all === "1" ? "/admin/messaging" : "/admin/messaging?all=1"} className="text-sm text-brand-700 hover:underline">
                {all === "1" ? t("showOpen") : t("showAll")}
              </Link>
            </div>
            {reports && reports.length > 0 ? (
              <ul className="mt-4 space-y-3">
                {reports.map((r) => (
                  <li key={r.id} className="rounded-2xl border border-ink-100 bg-white p-5">
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge tone="accent">{tMessages(`reason.${r.reason}`)}</Badge>
                      {r.resolvedAt && <Badge>{t("resolved")}</Badge>}
                      <span className="text-xs text-ink-400">{date.format(new Date(r.createdAt))}</span>
                    </div>
                    {r.details && <p className="mt-2 text-sm text-ink-800">{r.details}</p>}
                    <dl className="mt-3 grid gap-1 text-sm text-ink-600 sm:grid-cols-2">
                      <div>
                        <dt className="inline font-medium">{t("reporter")}: </dt>
                        <dd className="inline">{who(r.reporter)}</dd>
                      </div>
                      <div>
                        <dt className="inline font-medium">{t("reportedUser")}: </dt>
                        <dd className="inline">
                          {who(r.reportedUser)} {r.reportedUser.isBannedFromMessaging && <Badge tone="accent">{t("banned")}</Badge>}
                        </dd>
                      </div>
                      <div className="sm:col-span-2">
                        <dt className="inline font-medium">{t("listing")}: </dt>
                        <dd className="inline">{r.listing.title ?? tMessages("listingDeleted")}</dd>
                      </div>
                    </dl>
                    <div className="mt-4 flex flex-wrap items-center gap-3">
                      <Link href={`/admin/messaging/conversations/${r.conversationId}`} className="text-sm font-medium text-brand-700 hover:underline">
                        {t("viewConversation")}
                      </Link>
                      <MessagingAdminActions reportId={r.resolvedAt ? undefined : r.id} user={r.reportedUser} />
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="mt-4 text-sm text-ink-500">{t("noReports")}</p>
            )}
          </section>

          <section className="mt-12">
            <h2 className="font-display text-xl font-medium text-ink-950">{t("flagged")}</h2>
            <p className="mt-1 text-sm text-ink-500">{t("flaggedHint")}</p>
            {flagged && flagged.length > 0 ? (
              <ul className="mt-4 space-y-3">
                {flagged.map((f) => (
                  <li key={f.message.id} className="rounded-2xl border border-ink-100 bg-white p-5">
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge tone="accent">{f.flagReason}</Badge>
                      <span className="text-xs text-ink-400">{date.format(new Date(f.message.createdAt))}</span>
                      <span className="text-sm text-ink-600">{who(f.sender)}</span>
                      {f.sender.isBannedFromMessaging && <Badge tone="accent">{t("banned")}</Badge>}
                    </div>
                    <p className="mt-2 whitespace-pre-wrap text-sm text-ink-800">{f.message.body}</p>
                    <div className="mt-4 flex flex-wrap items-center gap-3">
                      <Link href={`/admin/messaging/conversations/${f.message.conversationId}`} className="text-sm font-medium text-brand-700 hover:underline">
                        {t("viewConversation")}
                      </Link>
                      <MessagingAdminActions user={f.sender} />
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="mt-4 text-sm text-ink-500">{t("noFlagged")}</p>
            )}
          </section>
        </div>
      </main>
      <Footer />
    </div>
  );
}
