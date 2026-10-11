"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { renewListing } from "@/lib/property/actions";

// "Prelungește" in the needs-attention list: renews the listing for another 6 months, then the
// page reloads its numbers (the listing leaves the list).
export function RenewListingButton({ listingId }: { listingId: string }) {
  const t = useTranslations("AgencyStats");
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);

  return (
    <div className="flex flex-col items-end gap-1">
      <Button
        variant="secondary"
        size="sm"
        disabled={pending}
        onClick={() =>
          startTransition(async () => {
            const result = await renewListing(listingId);
            if (result.error) {
              setError(result.error);
            } else {
              setError(null);
              router.refresh();
            }
          })
        }
      >
        {pending ? t("renewing") : t("renew")}
      </Button>
      {error && (
        <p role="alert" className="text-xs text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}
