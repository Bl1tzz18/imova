"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { HeirChoice } from "@/components/agency/manage/MembersPanel";
import { canLeave, defaultHeir, heirOptions, MY_AGENCIES_HREF } from "@/lib/agency/manage";
import { removeMember } from "@/lib/agency/manageActions";
import type { AgencyMember } from "@/types/agency";

// The public page's address with a copy button (the address stays selectable when the clipboard
// isn't available).
export function CopyLinkButton({ url }: { url: string }) {
  const t = useTranslations("AgencyManage");
  const [copied, setCopied] = useState(false);

  async function copy() {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // No clipboard (an insecure origin, a refused permission): the address is right there.
    }
  }

  return (
    <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
      <input
        readOnly
        value={url}
        aria-label={t("publicPageLabel")}
        onFocus={(e) => e.target.select()}
        className="h-11 min-w-0 flex-1 rounded-xl border border-ink-200 bg-ink-50 px-3.5 text-sm text-ink-700 outline-none focus:border-brand-500"
      />
      <Button type="button" variant="secondary" onClick={copy} aria-live="polite">
        {copied ? t("copied") : t("copyLink")}
      </Button>
    </div>
  );
}

// "Părăsește agenția": asks first, says what happens to the caller's listings and lets them pick who
// takes them over. The last Owner can't leave — they see why instead of a button.
export function LeaveAgencySection({
  agencyId,
  agencyName,
  userId,
  members,
}: {
  agencyId: string;
  agencyName: string;
  userId: string;
  members: AgencyMember[];
}) {
  const t = useTranslations("AgencyManage");
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const me = members.find((m) => m.userId === userId);
  const heirs = heirOptions(members, userId);
  const [heir, setHeir] = useState(() => defaultHeir(members, userId, null) ?? "");

  if (!me) return null;
  const listingCount = me.listingCount;

  function leave() {
    setError(null);
    startTransition(async () => {
      const result = await removeMember(agencyId, userId, listingCount > 0 ? heir || null : null);
      if (result.error) {
        setError(result.error);
        return;
      }
      router.push(MY_AGENCIES_HREF);
      router.refresh();
    });
  }

  return (
    <section aria-labelledby="agency-leave-title" className="rounded-2xl border border-ink-100 p-4 sm:p-5">
      <h2 id="agency-leave-title" className="font-display text-lg font-medium text-ink-950">
        {t("leaveTitle")}
      </h2>
      {canLeave(members, userId) ? (
        <>
          <p className="mt-1 text-sm text-ink-500">{t("leaveIntro")}</p>
          <Button type="button" variant="secondary" className="mt-4" onClick={() => setOpen(true)}>
            {t("leaveButton")}
          </Button>
        </>
      ) : (
        <p className="mt-1 text-sm text-ink-500">{t("leaveLastOwner")}</p>
      )}

      <ConfirmDialog
        open={open}
        danger
        title={t("leaveConfirmTitle", { agency: agencyName })}
        confirmLabel={t("leaveConfirm")}
        cancelLabel={t("cancel")}
        pendingLabel={t("leaving")}
        pending={pending}
        error={error}
        onConfirm={leave}
        onCancel={() => {
          setOpen(false);
          setError(null);
        }}
      >
        <HeirChoice self listingCount={listingCount} heirs={heirs} heir={heir} onChange={setHeir} />
      </ConfirmDialog>
    </section>
  );
}
