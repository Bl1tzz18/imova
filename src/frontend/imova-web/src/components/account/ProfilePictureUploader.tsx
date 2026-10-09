"use client";

import { useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { Avatar } from "@/components/ui/Avatar";
import { PROFILE_PICTURE_MAX_BYTES } from "@/lib/api/uploadSize";

// Same allow-list/size-limit UX as ImageUploader.tsx (listing photos) — extension is only used
// for the client-side check; the backend is what actually validates via magic-byte sniffing.
const ALLOWED_EXTENSIONS = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".heic", ".heif"];

type PictureResult = { profilePictureUrl?: string | null; error?: string };

// Upload and removal go through the route handler /account/profile-picture (a server action would
// stop the photo at 1 MB; phone photos are 3–5 MB).
async function sendPicture(init: RequestInit, failed: string): Promise<PictureResult> {
  try {
    const res = await fetch("/account/profile-picture", init);
    return (await res.json()) as PictureResult;
  } catch {
    return { error: failed };
  }
}

function getAllowedExtension(fileName: string): string | null {
  const match = /\.[a-zA-Z0-9]+$/.exec(fileName);
  if (!match) return null;
  const ext = match[0].toLowerCase();
  return ALLOWED_EXTENSIONS.includes(ext) ? ext : null;
}

export function ProfilePictureUploader({
  userId,
  displayName,
  email,
  profilePictureUrl,
}: {
  userId: string;
  displayName: string | null;
  email: string;
  profilePictureUrl: string | null;
}) {
  const t = useTranslations("Account");
  const router = useRouter();
  const inputRef = useRef<HTMLInputElement>(null);
  const [pictureUrl, setPictureUrl] = useState(profilePictureUrl);
  const [pending, setPending] = useState<"upload" | "remove" | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [confirmingRemove, setConfirmingRemove] = useState(false);

  function handleFile(files: FileList | null) {
    const file = files?.[0];
    if (!file) return;

    if (!getAllowedExtension(file.name)) {
      setError(t("pictureInvalidType"));
      return;
    }
    if (file.size > PROFILE_PICTURE_MAX_BYTES) {
      setError(t("pictureTooLarge"));
      return;
    }

    setError(null);
    const localPreview = URL.createObjectURL(file);
    setPictureUrl(localPreview);
    setPending("upload");

    void (async () => {
      const body = new FormData();
      body.append("file", file);
      const result = await sendPicture({ method: "POST", body }, t("pictureUploadFailed"));
      setPending(null);

      if (result.error) {
        setError(result.error);
        setPictureUrl(profilePictureUrl);
        return;
      }

      setPictureUrl(result.profilePictureUrl ?? localPreview);
      router.refresh();
    })();
  }

  function handleConfirmRemove() {
    setConfirmingRemove(false);
    setError(null);
    setPending("remove");

    void (async () => {
      const result = await sendPicture({ method: "DELETE" }, t("pictureUploadFailed"));
      setPending(null);

      if (result.error) {
        setError(result.error);
        return;
      }

      setPictureUrl(null);
      router.refresh();
    })();
  }

  return (
    <div className="flex items-center gap-4">
      <Avatar userId={userId} displayName={displayName} email={email} pictureUrl={pictureUrl} size={56} />

      <div>
        {confirmingRemove ? (
          <div className="flex items-center gap-2">
            <span className="text-sm text-ink-600">{t("removePictureConfirm")}</span>
            <button
              type="button"
              onClick={handleConfirmRemove}
              className="inline-flex h-9 items-center justify-center rounded-full bg-accent-600 px-3.5 text-sm font-medium text-white transition-colors hover:bg-accent-700"
            >
              {t("removePictureConfirmYes")}
            </button>
            <button
              type="button"
              onClick={() => setConfirmingRemove(false)}
              className="inline-flex h-9 items-center justify-center rounded-full px-3.5 text-sm font-medium text-ink-500 transition-colors hover:bg-ink-50"
            >
              {t("removePictureCancel")}
            </button>
          </div>
        ) : (
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={() => inputRef.current?.click()}
              disabled={pending !== null}
              className="inline-flex h-10 items-center justify-center rounded-full border border-ink-200 bg-white px-4 text-sm font-medium text-ink-900 transition-colors hover:border-ink-300 hover:bg-ink-50 disabled:opacity-50"
            >
              {pending === "upload" ? t("uploadingPicture") : t("changePicture")}
            </button>

            {pictureUrl && (
              <button
                type="button"
                onClick={() => setConfirmingRemove(true)}
                disabled={pending !== null}
                className="inline-flex h-10 items-center justify-center rounded-full px-3.5 text-sm font-medium text-ink-500 transition-colors hover:bg-ink-50 hover:text-accent-600 disabled:opacity-50"
              >
                {pending === "remove" ? t("removingPicture") : t("removePicture")}
              </button>
            )}
          </div>
        )}
        <input
          ref={inputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp,image/gif,image/bmp,image/heic,image/heif,.jpg,.jpeg,.png,.webp,.gif,.bmp,.heic,.heif"
          className="sr-only"
          onChange={(e) => {
            handleFile(e.target.files);
            e.target.value = "";
          }}
        />
        {error && <p className="mt-1.5 text-xs text-accent-600">{error}</p>}
      </div>
    </div>
  );
}
