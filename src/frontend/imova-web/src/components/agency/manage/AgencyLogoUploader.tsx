"use client";

import { useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { Avatar } from "@/components/ui/Avatar";
import { buttonClassName } from "@/components/ui/Button";
import { cn } from "@/lib/utils/cn";

// What the API takes for a logo (it checks the bytes, these only spare a pointless upload).
const ALLOWED_EXTENSIONS = [".jpg", ".jpeg", ".png", ".webp"];
const MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024;

type LogoResponse = { logoUrl: string | null; logoThumbnailUrl: string | null } | { error: string };

// The agency's logo: upload (the server pads it to a white square — nothing to crop), replace,
// remove (asks first). Goes through /account/agencies/[id]/logo; the page refreshes afterwards so
// the header and the preview show the new one. `highlight`: just created — ask for a logo.
export function AgencyLogoUploader({
  agencyId,
  name,
  logoUrl,
  highlight = false,
}: {
  agencyId: string;
  name: string;
  logoUrl: string | null;
  highlight?: boolean;
}) {
  const t = useTranslations("AgencyManage");
  const router = useRouter();
  const input = useRef<HTMLInputElement>(null);
  const [preview, setPreview] = useState(logoUrl);
  const [pending, setPending] = useState<"upload" | "remove" | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [confirmingRemove, setConfirmingRemove] = useState(false);

  async function send(init: RequestInit): Promise<LogoResponse> {
    try {
      const res = await fetch(`/account/agencies/${encodeURIComponent(agencyId)}/logo`, init);
      return (await res.json()) as LogoResponse;
    } catch {
      return { error: t("logoFailed") };
    }
  }

  async function upload(file: File) {
    const extension = /\.[a-z0-9]+$/i.exec(file.name)?.[0].toLowerCase();
    if (!extension || !ALLOWED_EXTENSIONS.includes(extension)) {
      setError(t("logoWrongType"));
      return;
    }
    if (file.size > MAX_FILE_SIZE_BYTES) {
      setError(t("logoTooLarge"));
      return;
    }

    setError(null);
    setPending("upload");
    const local = URL.createObjectURL(file);
    setPreview(local);

    const body = new FormData();
    body.append("file", file);
    const result = await send({ method: "POST", body });
    setPending(null);
    URL.revokeObjectURL(local);

    if ("error" in result) {
      setError(result.error);
      setPreview(logoUrl);
      return;
    }
    setPreview(result.logoUrl);
    router.refresh();
  }

  async function remove() {
    setConfirmingRemove(false);
    setError(null);
    setPending("remove");
    const result = await send({ method: "DELETE" });
    setPending(null);

    if ("error" in result) {
      setError(result.error);
      return;
    }
    setPreview(null);
    router.refresh();
  }

  return (
    <div
      className={cn(
        "flex flex-col gap-4 rounded-2xl border p-4 sm:flex-row sm:items-center",
        highlight && !preview ? "border-brand-200 bg-brand-50/60" : "border-ink-100",
      )}
    >
      <Avatar
        userId={agencyId}
        displayName={name}
        pictureUrl={preview}
        size={80}
        shape="square"
        className={cn("shrink-0 border border-ink-100", pending === "upload" && "opacity-60")}
      />

      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium text-ink-900">{t("logoTitle")}</p>
        <p className="mt-0.5 text-sm text-ink-500">{highlight && !preview ? t("logoPrompt") : t("logoHint")}</p>

        {confirmingRemove ? (
          <div className="mt-3 flex flex-wrap items-center gap-2">
            <span className="text-sm text-ink-700">{t("logoRemoveConfirm")}</span>
            <button type="button" onClick={remove} className={buttonClassName({ variant: "danger", size: "sm" })}>
              {t("logoRemoveYes")}
            </button>
            <button type="button" onClick={() => setConfirmingRemove(false)} className={buttonClassName({ variant: "secondary", size: "sm" })}>
              {t("cancel")}
            </button>
          </div>
        ) : (
          <div className="mt-3 flex flex-wrap items-center gap-2">
            <button
              type="button"
              onClick={() => input.current?.click()}
              disabled={pending !== null}
              className={buttonClassName({ variant: preview ? "secondary" : "primary", size: "sm" })}
            >
              {pending === "upload" ? t("logoUploading") : preview ? t("logoChange") : t("logoUpload")}
            </button>
            {preview && (
              <button
                type="button"
                onClick={() => setConfirmingRemove(true)}
                disabled={pending !== null}
                className={buttonClassName({ variant: "ghost", size: "sm", className: "hover:bg-ink-50 hover:text-accent-700" })}
              >
                {pending === "remove" ? t("logoRemoving") : t("logoRemove")}
              </button>
            )}
          </div>
        )}

        <input
          ref={input}
          type="file"
          accept="image/jpeg,image/png,image/webp,.jpg,.jpeg,.png,.webp"
          className="sr-only"
          // Opened by the button above; not a second control of its own.
          tabIndex={-1}
          aria-hidden
          onChange={(e) => {
            const file = e.target.files?.[0];
            e.target.value = "";
            if (file) void upload(file);
          }}
        />
        {error && (
          <p role="alert" className="mt-2 text-sm text-accent-700">
            {error}
          </p>
        )}
      </div>
    </div>
  );
}
