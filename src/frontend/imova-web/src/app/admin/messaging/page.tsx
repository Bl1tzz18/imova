import Link from "next/link";
import { redirect } from "next/navigation";
import { getLocale, getTranslations } from "next-intl/server";
import { Footer } from "@/components/layout/Footer";
import { MessagingAdminActions } from "@/components/admin/MessagingAdminActions";
import { Badge } from "@/components/ui/Badge";
import { getCurrentUserProfile } from "@/lib/auth/profile";
import { adminViewHref, parseAdminView, type AdminView } from "@/lib/messaging/adminView";
import { getFlaggedMessages, getMessagingReports } from "@/lib/messaging/api";
import { cn } from "@/lib/utils/cn";
import type { MessagingUser } from "@/types/messaging";

function who(user: MessagingUser) {
  return `${user.displayName ?? "—"}${user.email ? ` (${user.email})` : ""}`;
}

// Moderation for messaging, in two tabs: Active (reports and auto-flagged messages still to look
// at) and Resolved (already handled — each can be reopened).
export default async function AdminMessagingPage({ searchParams }: { searchParams: Promise<{ view?: string }> }) {
  const view = parseAdminView((await searchParams).view);
  const profile = await getCurrentUserProfile();
  if (!profile) {
    redirect(`/login?next=${encodeURIComponent(adminViewHref(view))}`);
  }

  const [t, tMessages, locale] = await Promise.all([getTranslations("AdminMessaging"), getTranslations("Messages"), getLocale()]);
  if (!profile.roles.includes("Admin")) {
    return (
      <main className="mx-auto max-w-2xl px-4 py-16 text-center sm:px-6">
        <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">{t("forbidden")}</p>
      </main>
    );
  }

  // Both tabs' lists, so each tab can show its count.
  const [activeReports, resolvedReports, activeFlagged, resolvedFlagged] = await Promise.all([
    getMessagingReports(false),
    getMessagingReports(true),
    getFlaggedMessages(false),
    getFlaggedMessages(true),
  ]);
  const counts: Record<AdminView, number> = {
    active: (activeReports?.length ?? 0) + (activeFlagged?.length ?? 0),
    resolved: (resolvedReports?.length ?? 0) + (resolvedFlagged?.length ?? 0),
  };
  const resolved = view === "resolved";
  const reports = (resolved ? resolvedReports : activeReports) ?? [];
  const flagged = (resolved ? resolvedFlagged : activeFlagged) ?? [];
  const date = new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "short" });
  const resolvedLine = (by: MessagingUser | null, at: string | null) =>
    at && (
      <p className="mt-2 text-xs text-ink-500">
        {t("resolvedBy", { name: by?.displayName ?? by?.email ?? "—", date: date.format(new Date(at)) })}
      </p>
    );

  return (
    <div className="flex min-h-screen flex-col">
      <main className="flex-1">
        <div className="mx-auto max-w-5xl px-4 py-10 sm:px-6">
          <h1 className="font-hero text-3xl font-extrabold text-ink-950">{t("title")}</h1>

          <nav className="mt-6 flex gap-1 rounded-full border border-ink-100 bg-ink-50 p-1 sm:inline-flex" aria-label={t("title")}>
            {(["active", "resolved"] as const).map((tab) => (
              <Link
                key={tab}
                href={adminViewHref(tab)}
                aria-current={view === tab ? "page" : undefined}
                className={cn(
                  "flex flex-1 items-center justify-center gap-2 rounded-full px-5 py-2 text-sm font-medium transition-colors sm:flex-none",
                  view === tab ? "bg-ink-950 text-white" : "text-ink-600 hover:bg-white",
                )}
              >
                {tab === "active" ? t("tabActive") : t("tabResolved")}
                <span className={cn("rounded-full px-2 py-0.5 text-xs", view === tab ? "bg-white/20" : "bg-ink-100")}>{counts[tab]}</span>
              </Link>
            ))}
          </nav>

          <section className="mt-8">
            <h2 className="font-display text-xl font-medium text-ink-950">{t("reports")}</h2>
            {reports.length > 0 ? (
              <ul className="mt-4 space-y-3">
                {reports.map((r) => (
                  <li key={r.id} className="rounded-2xl border border-ink-100 bg-white p-5">
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge tone="accent">{tMessages(`reason.${r.reason}`)}</Badge>
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
                    {resolvedLine(r.resolvedBy, r.resolvedAt)}
                    <div className="mt-4 flex flex-wrap items-center gap-3">
                      <Link href={`/admin/messaging/conversations/${r.conversationId}`} className="text-sm font-medium text-brand-700 hover:underline">
                        {t("viewConversation")}
                      </Link>
                      <MessagingAdminActions item={{ kind: "report", id: r.id, resolved }} user={r.reportedUser} />
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="mt-4 text-sm text-ink-500">{resolved ? t("noResolvedReports") : t("noActiveReports")}</p>
            )}
          </section>

          <section className="mt-12">
            <h2 className="font-display text-xl font-medium text-ink-950">{t("flagged")}</h2>
            {!resolved && <p className="mt-1 text-sm text-ink-500">{t("flaggedHint")}</p>}
            {flagged.length > 0 ? (
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
                    {resolvedLine(f.resolvedBy, f.resolvedAt)}
                    <div className="mt-4 flex flex-wrap items-center gap-3">
                      <Link href={`/admin/messaging/conversations/${f.message.conversationId}`} className="text-sm font-medium text-brand-700 hover:underline">
                        {t("viewConversation")}
                      </Link>
                      <MessagingAdminActions item={{ kind: "flag", id: f.message.id, resolved }} user={f.sender} />
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="mt-4 text-sm text-ink-500">{resolved ? t("noResolvedFlagged") : t("noActiveFlagged")}</p>
            )}
          </section>
        </div>
      </main>
      <Footer />
    </div>
  );
}
