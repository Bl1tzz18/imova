"use client";

import { useEffect, useRef, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { SelectInput, TextAreaInput } from "@/components/ui/Field";
import { reportConversation, setConversationArchived, setUserBlocked } from "@/lib/messaging/actions";
import { cn } from "@/lib/utils/cn";
import type { ReportReason } from "@/types/messaging";

const REASONS: ReportReason[] = ["Spam", "Fraud", "Abuse", "Other"];

const iconButton =
  "flex h-9 w-9 items-center justify-center rounded-full text-ink-500 transition-colors hover:bg-bubble hover:text-ink-900 disabled:opacity-50";

// The thread header's actions, all behind one "⋯" button: archive, block, and — after a divider,
// in red since it flags the other person — report (which opens its form as a popover).
export function ConversationActions({
  conversationId,
  isArchived,
  blockedByMe,
  onBlockedChange,
}: {
  conversationId: string;
  isArchived: boolean;
  blockedByMe: boolean;
  onBlockedChange: (blocked: boolean) => void;
}) {
  const t = useTranslations("Messages");
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [panel, setPanel] = useState<"menu" | "report" | null>(null);
  const [reason, setReason] = useState<ReportReason>("Spam");
  const [details, setDetails] = useState("");
  const [notice, setNotice] = useState<string | null>(null);
  const container = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!panel) return;
    function close(e: MouseEvent | KeyboardEvent) {
      if (e instanceof KeyboardEvent ? e.key === "Escape" : !container.current?.contains(e.target as Node)) setPanel(null);
    }
    document.addEventListener("mousedown", close);
    document.addEventListener("keydown", close);
    return () => {
      document.removeEventListener("mousedown", close);
      document.removeEventListener("keydown", close);
    };
  }, [panel]);

  useEffect(() => {
    if (!notice) return;
    const timer = setTimeout(() => setNotice(null), 4000);
    return () => clearTimeout(timer);
  }, [notice]);

  function run(action: () => Promise<{ error?: string }>, onDone?: () => void) {
    startTransition(async () => {
      const result = await action();
      if (result.error) {
        setNotice(result.error);
        return;
      }
      onDone?.();
      router.refresh();
    });
  }

  const menuItem =
    "flex w-full items-center gap-3 px-4 py-2.5 text-left text-sm text-ink-800 transition-colors hover:bg-bubble disabled:opacity-50";
  const itemIcon = "h-[18px] w-[18px] shrink-0";

  return (
    <div ref={container} className="relative flex items-center">
      <button
        type="button"
        className={cn(iconButton, panel && "bg-bubble text-ink-900")}
        aria-label={t("moreActions")}
        title={t("moreActions")}
        aria-haspopup="menu"
        aria-expanded={panel === "menu"}
        onClick={() => setPanel((p) => (p ? null : "menu"))}
      >
        <svg viewBox="0 0 24 24" fill="currentColor" className="h-[18px] w-[18px]" aria-hidden>
          <circle cx="5" cy="12" r="1.8" />
          <circle cx="12" cy="12" r="1.8" />
          <circle cx="19" cy="12" r="1.8" />
        </svg>
      </button>

      {panel === "menu" && (
        <div role="menu" className="absolute right-0 top-full z-20 mt-2 w-56 overflow-hidden rounded-xl border border-line bg-white py-1 shadow-[var(--shadow-card)]">
          <button
            type="button"
            role="menuitem"
            className={menuItem}
            disabled={pending}
            onClick={() => {
              setPanel(null);
              run(() => setConversationArchived(conversationId, !isArchived), () => setNotice(isArchived ? t("unarchived") : t("archived")));
            }}
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" className={cn(itemIcon, "text-ink-500")} aria-hidden>
              <path d="M3.5 5h17v4h-17zM5 9v10h14V9M10 13h4" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            {isArchived ? t("unarchive") : t("archiveAction")}
          </button>
          <button
            type="button"
            role="menuitem"
            className={menuItem}
            disabled={pending}
            onClick={() => {
              setPanel(null);
              if (!blockedByMe && !window.confirm(t("blockConfirm"))) return;
              run(() => setUserBlocked(conversationId, !blockedByMe), () => onBlockedChange(!blockedByMe));
            }}
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" className={cn(itemIcon, "text-ink-500")} aria-hidden>
              <circle cx="12" cy="12" r="8.5" />
              <path d="m6 6 12 12" strokeLinecap="round" />
            </svg>
            {blockedByMe ? t("unblock") : t("block")}
          </button>
          <div role="separator" className="my-1 h-px bg-line" />
          <button type="button" role="menuitem" className={cn(menuItem, "text-red-600 hover:bg-red-50")} onClick={() => setPanel("report")}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" className={itemIcon} aria-hidden>
              <path d="M12 4 2.8 19.5h18.4L12 4ZM12 10v4.5M12 17.2v.1" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            {t("report")}
          </button>
        </div>
      )}

      {panel === "report" && (
        <div className="absolute right-0 top-full z-20 mt-2 w-80 rounded-2xl border border-line bg-white p-4 text-left shadow-[var(--shadow-card)]">
          <label className="block text-sm font-medium text-ink-700">
            {t("reportReason")}
            <SelectInput value={reason} onChange={(e) => setReason(e.target.value as ReportReason)} className="mt-1">
              {REASONS.map((r) => (
                <option key={r} value={r}>
                  {t(`reason.${r}`)}
                </option>
              ))}
            </SelectInput>
          </label>
          <label className="mt-3 block text-sm font-medium text-ink-700">
            {reason === "Other" ? t("reportDetailsRequired") : t("reportDetails")}
            <TextAreaInput value={details} onChange={(e) => setDetails(e.target.value)} maxLength={1000} rows={3} className="mt-1" />
          </label>
          <div className="mt-3 flex justify-end gap-2">
            <Button type="button" size="sm" variant="ghost" onClick={() => setPanel(null)}>
              {t("cancel")}
            </Button>
            <Button
              type="button"
              size="sm"
              disabled={pending || (reason === "Other" && !details.trim())}
              onClick={() =>
                run(() => reportConversation(conversationId, reason, details), () => {
                  setPanel(null);
                  setDetails("");
                  setNotice(t("reported"));
                })
              }
            >
              {t("sendReport")}
            </Button>
          </div>
        </div>
      )}

      {notice && (
        <p role="status" className="absolute right-0 top-full z-10 mt-2 w-64 rounded-xl bg-ink-900 px-3 py-2 text-xs text-white shadow-[var(--shadow-card)]">
          {notice}
        </p>
      )}
    </div>
  );
}
