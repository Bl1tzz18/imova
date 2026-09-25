"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { resolveMessagingReport, setMessagingBan } from "@/lib/messaging/actions";
import type { MessagingUser } from "@/types/messaging";

// Resolve a report and/or ban (unban) a user from messaging.
export function MessagingAdminActions({ reportId, user }: { reportId?: string; user: MessagingUser }) {
  const t = useTranslations("AdminMessaging");
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);

  function run(action: () => Promise<{ error?: string }>) {
    startTransition(async () => {
      const result = await action();
      setError(result.error ?? null);
      if (!result.error) router.refresh();
    });
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      {reportId && (
        <Button type="button" size="sm" variant="secondary" disabled={pending} onClick={() => run(() => resolveMessagingReport(reportId))}>
          {t("resolve")}
        </Button>
      )}
      <Button
        type="button"
        size="sm"
        variant={user.isBannedFromMessaging ? "secondary" : "primary"}
        disabled={pending}
        onClick={() => {
          if (!user.isBannedFromMessaging && !window.confirm(t("banConfirm", { name: user.displayName ?? user.email ?? "" }))) return;
          run(() => setMessagingBan(user.id, !user.isBannedFromMessaging));
        }}
      >
        {user.isBannedFromMessaging ? t("unban", { name: user.displayName ?? user.email ?? "" }) : t("ban", { name: user.displayName ?? user.email ?? "" })}
      </Button>
      {error && <span className="text-sm text-accent-700">{error}</span>}
    </div>
  );
}
