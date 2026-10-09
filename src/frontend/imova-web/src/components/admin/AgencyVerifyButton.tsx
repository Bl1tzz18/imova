"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { setAgencyVerified } from "@/lib/admin/agencyActions";

// "Verifică" (at once — the owners get an email) or "Retrage verificarea" (asks first).
export function AgencyVerifyButton({ agencyId, agencyName, verified }: { agencyId: string; agencyName: string; verified: boolean }) {
  const t = useTranslations("AdminAgencies");
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [confirming, setConfirming] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function apply(next: boolean) {
    setError(null);
    startTransition(async () => {
      const result = await setAgencyVerified(agencyId, next);
      if (result.error) {
        setError(result.error);
        return;
      }
      setConfirming(false);
      router.refresh();
    });
  }

  return (
    <div className="flex flex-col items-start gap-1 sm:items-end">
      <Button
        type="button"
        size="sm"
        variant={verified ? "secondary" : "primary"}
        disabled={pending}
        onClick={() => (verified ? setConfirming(true) : apply(true))}
      >
        {pending ? t("saving") : verified ? t("unverify") : t("verify")}
      </Button>
      {error && !confirming && (
        <p role="alert" className="text-xs text-accent-700">
          {error}
        </p>
      )}
      <ConfirmDialog
        open={confirming}
        title={t("unverifyTitle", { name: agencyName })}
        confirmLabel={t("unverify")}
        cancelLabel={t("cancel")}
        pendingLabel={t("saving")}
        pending={pending}
        error={confirming ? error : null}
        onConfirm={() => apply(false)}
        onCancel={() => {
          setConfirming(false);
          setError(null);
        }}
      >
        {t("unverifyBody")}
      </ConfirmDialog>
    </div>
  );
}
