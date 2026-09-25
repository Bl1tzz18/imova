"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { SelectInput, TextAreaInput } from "@/components/ui/Field";
import { reportConversation, setConversationArchived, setUserBlocked } from "@/lib/messaging/actions";
import type { ReportReason } from "@/types/messaging";

const REASONS: ReportReason[] = ["Spam", "Fraud", "Abuse", "Other"];

// Archive / block / report for one conversation.
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
  const [reporting, setReporting] = useState(false);
  const [reason, setReason] = useState<ReportReason>("Spam");
  const [details, setDetails] = useState("");
  const [notice, setNotice] = useState<string | null>(null);

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

  return (
    <div className="flex flex-col items-end gap-2">
      <div className="flex flex-wrap justify-end gap-2">
        <Button
          type="button"
          size="sm"
          variant="secondary"
          disabled={pending}
          onClick={() => run(() => setConversationArchived(conversationId, !isArchived), () => setNotice(isArchived ? t("unarchived") : t("archived")))}
        >
          {isArchived ? t("unarchive") : t("archive")}
        </Button>
        <Button
          type="button"
          size="sm"
          variant="secondary"
          disabled={pending}
          onClick={() => {
            if (!blockedByMe && !window.confirm(t("blockConfirm"))) return;
            run(() => setUserBlocked(conversationId, !blockedByMe), () => onBlockedChange(!blockedByMe));
          }}
        >
          {blockedByMe ? t("unblock") : t("block")}
        </Button>
        <Button type="button" size="sm" variant="ghost" disabled={pending} onClick={() => setReporting((v) => !v)}>
          {t("report")}
        </Button>
      </div>

      {reporting && (
        <div className="w-full max-w-sm rounded-xl border border-ink-100 bg-white p-4 text-left shadow-[var(--shadow-card)]">
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
            <Button type="button" size="sm" variant="ghost" onClick={() => setReporting(false)}>
              {t("cancel")}
            </Button>
            <Button
              type="button"
              size="sm"
              disabled={pending || (reason === "Other" && !details.trim())}
              onClick={() =>
                run(() => reportConversation(conversationId, reason, details), () => {
                  setReporting(false);
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

      {notice && <p className="text-sm text-ink-600">{notice}</p>}
    </div>
  );
}
