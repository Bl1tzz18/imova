"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { adminDeleteAgency } from "@/lib/admin/agencyActions";

const MAX_REASON = 1000;

// A site admin deletes an agency from /admin/agencies: a quiet red "Șterge" on the row, then a dialog
// that says exactly what happens (members lose access and get an email with the reason; the listings
// stay, as their authors' personal listings; the page, logo, invitations and imports go; no undo),
// asks for the reason and the agency's exact name, and only then enables the delete.
export function AgencyAdminDeleteButton({
  agencyId,
  agencyName,
  memberCount,
  activeListingCount,
}: {
  agencyId: string;
  agencyName: string;
  memberCount: number;
  activeListingCount: number;
}) {
  const t = useTranslations("AdminAgencies");
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [typed, setTyped] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();
  const ready = reason.trim().length > 0 && typed.trim() === agencyName;

  function close() {
    setOpen(false);
    setReason("");
    setTyped("");
    setError(null);
  }

  function remove() {
    setError(null);
    startTransition(async () => {
      const result = await adminDeleteAgency(agencyId, typed.trim(), reason.trim());
      if (result.error) {
        setError(result.error);
        return;
      }
      close();
      router.refresh();
    });
  }

  const field =
    "w-full rounded-xl border border-ink-200 bg-white px-3.5 text-sm text-ink-900 outline-none focus:border-red-500 focus:ring-2 focus:ring-red-500/20";

  return (
    <>
      <button type="button" onClick={() => setOpen(true)} className="py-2 text-sm font-medium text-red-600 hover:text-red-700">
        {t("delete")}
      </button>
      <ConfirmDialog
        open={open}
        title={t("deleteTitle", { name: agencyName })}
        confirmLabel={t("deleteConfirm")}
        cancelLabel={t("cancel")}
        pendingLabel={t("deleting")}
        pending={pending}
        danger
        confirmDisabled={!ready}
        error={error}
        onConfirm={remove}
        onCancel={close}
      >
        <ul className="list-disc space-y-1 pl-5">
          <li>{t("deleteMembers", { count: memberCount })}</li>
          <li>{t("deleteListings", { count: activeListingCount })}</li>
          <li>{t("deleteOther")}</li>
          <li className="font-medium text-ink-800">{t("deleteIrreversible")}</li>
        </ul>
        <label className="mt-4 block">
          <span className="mb-1.5 block font-medium text-ink-700">{t("deleteReasonLabel")}</span>
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            maxLength={MAX_REASON}
            rows={3}
            className={`${field} resize-y py-2.5`}
          />
          <span className="mt-1 block text-xs text-ink-500">{t("deleteReasonHint")}</span>
        </label>
        <label className="mt-3 block">
          <span className="mb-1.5 block font-medium text-ink-700">{t("deleteNameLabel", { name: agencyName })}</span>
          <input
            value={typed}
            onChange={(e) => setTyped(e.target.value)}
            autoComplete="off"
            spellCheck={false}
            className={`${field} h-11`}
          />
        </label>
      </ConfirmDialog>
    </>
  );
}
