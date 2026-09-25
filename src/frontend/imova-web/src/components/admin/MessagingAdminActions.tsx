"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { setFlaggedMessageResolved, setMessagingBan, setMessagingReportResolved } from "@/lib/messaging/actions";
import type { MessagingUser } from "@/types/messaging";

// What the Resolve / Reopen button acts on — omitted where there's nothing to resolve (the
// conversation view's per-participant ban buttons).
export type ResolvableItem = { kind: "report" | "flag"; id: string; resolved: boolean };

// Resolve (or reopen) a report / flagged message, and ban (or unban) a user from messaging.
export function MessagingAdminActions({ item, user }: { item?: ResolvableItem; user: MessagingUser }) {
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

  function toggleResolved(target: ResolvableItem) {
    const resolved = !target.resolved;
    run(() =>
      target.kind === "report" ? setMessagingReportResolved(target.id, resolved) : setFlaggedMessageResolved(target.id, resolved),
    );
  }

  const name = user.displayName ?? user.email ?? "";
  return (
    <div className="flex flex-wrap items-center gap-2">
      {item && (
        <Button type="button" size="sm" variant={item.resolved ? "secondary" : "primary"} disabled={pending} onClick={() => toggleResolved(item)}>
          {item.resolved ? t("reopen") : t("resolve")}
        </Button>
      )}
      <Button
        type="button"
        size="sm"
        variant="secondary"
        disabled={pending}
        onClick={() => {
          if (!user.isBannedFromMessaging && !window.confirm(t("banConfirm", { name }))) return;
          run(() => setMessagingBan(user.id, !user.isBannedFromMessaging));
        }}
      >
        {user.isBannedFromMessaging ? t("unban", { name }) : t("ban", { name })}
      </Button>
      {error && <span className="text-sm text-accent-700">{error}</span>}
    </div>
  );
}
