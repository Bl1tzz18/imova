"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { confirmMediaUpload, requestUploadUrl, uploadFileToBlob } from "@/lib/api/media";
import { deletePropertyMedia } from "@/lib/property/actions";
import type { PropertyMedia } from "@/types/property";

const ALLOWED_EXTENSIONS = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".heic", ".heif"];
const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

// Keyed off the filename's extension rather than File.type: browsers often report an empty or
// unreliable MIME type for formats they can't natively decode (HEIC especially — Chrome/Firefox
// commonly report "" for .heic files even though it's a perfectly valid upload). The extension
// is only used to pick the blob name/SAS request here — the backend is what actually validates
// the file is what it claims via magic-byte sniffing (see ImageSignature), so a wrong guess here
// just surfaces as an upload error rather than a security gap.
function getAllowedExtension(fileName: string): string | null {
  const match = /\.[a-zA-Z0-9]+$/.exec(fileName);
  if (!match) return null;
  const ext = match[0].toLowerCase();
  return ALLOWED_EXTENSIONS.includes(ext) ? ext : null;
}

type UploadItem = {
  id: string;
  previewUrl: string;
  status: "uploading" | "done" | "deleting" | "error";
  error?: string;
  // Set once the row is actually persisted on the backend — either pre-loaded from an existing
  // listing's media (see `initialMedia`) or after a fresh upload's confirmMediaUpload resolves.
  // A remove on an item with a mediaId has to call the backend (deletePropertyMedia); a remove
  // on one still uploading/errored (no row exists yet) is purely local state.
  mediaId?: string;
};

// Uploads each selected image straight to Blob Storage via a short-lived SAS URL (see
// lib/api/media.ts), then tells the backend to confirm+validate it. `initialMedia` seeds already-
// uploaded photos (editing an existing listing) as already-"done" items so they show up alongside
// anything newly added, and can be removed the same way.
export function ImageUploader({
  propertyId,
  initialMedia,
}: {
  propertyId: string;
  initialMedia?: PropertyMedia[];
}) {
  const [items, setItems] = useState<UploadItem[]>(
    () =>
      initialMedia?.map((media) => ({
        id: media.id,
        mediaId: media.id,
        previewUrl: media.url,
        status: "done" as const,
      })) ?? [],
  );
  const t = useTranslations("PropertyForm");

  const handleFiles = (files: FileList | null) => {
    if (!files) return;

    for (const file of Array.from(files)) {
      const extension = getAllowedExtension(file.name);
      const id = crypto.randomUUID();

      if (!extension) {
        setItems((prev) => [
          ...prev,
          { id, previewUrl: "", status: "error", error: t("photoInvalidType") },
        ]);
        continue;
      }

      if (file.size > MAX_FILE_SIZE_BYTES) {
        setItems((prev) => [
          ...prev,
          { id, previewUrl: URL.createObjectURL(file), status: "error", error: t("photoTooLarge") },
        ]);
        continue;
      }

      const previewUrl = URL.createObjectURL(file);
      setItems((prev) => [...prev, { id, previewUrl, status: "uploading" }]);

      void (async () => {
        try {
          const { uploadUrl, blobName } = await requestUploadUrl(propertyId, extension);
          await uploadFileToBlob(uploadUrl, file);
          const confirmed = await confirmMediaUpload(propertyId, blobName);
          setItems((prev) => prev.map((it) => (it.id === id ? { ...it, status: "done", mediaId: confirmed.id } : it)));
        } catch {
          setItems((prev) =>
            prev.map((it) => (it.id === id ? { ...it, status: "error", error: t("photoUploadFailed") } : it))
          );
        }
      })();
    }
  };

  const removeItem = (item: UploadItem) => {
    if (item.status === "uploading" || item.status === "deleting") return;

    if (!item.mediaId) {
      setItems((prev) => prev.filter((it) => it.id !== item.id));
      return;
    }

    setItems((prev) => prev.map((it) => (it.id === item.id ? { ...it, status: "deleting" } : it)));

    void (async () => {
      const result = await deletePropertyMedia(propertyId, item.mediaId!);
      if (result.error) {
        setItems((prev) =>
          prev.map((it) => (it.id === item.id ? { ...it, status: "error", error: result.error } : it))
        );
        return;
      }
      setItems((prev) => prev.filter((it) => it.id !== item.id));
    })();
  };

  return (
    <div>
      <label className="flex cursor-pointer flex-col items-center justify-center gap-2 rounded-xl border border-dashed border-ink-300 bg-ink-50/50 px-4 py-8 text-center transition-colors hover:border-brand-400 hover:bg-brand-50/40">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" className="h-7 w-7 text-ink-400">
          <path d="M4 16.5V19a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-2.5M7 9l5-5 5 5M12 4v13" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
        <span className="text-sm font-medium text-ink-700">{t("addPhotos")}</span>
        <span className="text-xs text-ink-400">{t("photoHint")}</span>
        <input
          type="file"
          accept="image/jpeg,image/png,image/webp,image/gif,image/bmp,image/heic,image/heif,.jpg,.jpeg,.png,.webp,.gif,.bmp,.heic,.heif"
          multiple
          className="sr-only"
          onChange={(e) => {
            handleFiles(e.target.files);
            e.target.value = "";
          }}
        />
      </label>

      {items.length > 0 && (
        <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
          {items.map((item) => (
            <div key={item.id} className="relative aspect-square overflow-hidden rounded-xl border border-ink-100 bg-ink-100">
              {item.previewUrl && (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={item.previewUrl} alt="" className="h-full w-full object-cover" />
              )}

              {(item.status === "uploading" || item.status === "deleting") && (
                <div className="absolute inset-0 flex items-center justify-center bg-black/40">
                  <span className="h-5 w-5 animate-spin rounded-full border-2 border-white/40 border-t-white" />
                </div>
              )}

              {item.status === "error" && (
                <div className="absolute inset-0 flex items-center justify-center bg-red-900/70 p-2 text-center text-[11px] font-medium text-white">
                  {item.error}
                </div>
              )}

              <button
                type="button"
                onClick={() => removeItem(item)}
                disabled={item.status === "uploading" || item.status === "deleting"}
                aria-label={t("removePhoto")}
                className="absolute right-1.5 top-1.5 flex h-6 w-6 items-center justify-center rounded-full bg-red-500 text-white shadow-sm transition-colors hover:bg-red-600 disabled:pointer-events-none disabled:opacity-50"
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-3.5 w-3.5">
                  <path d="M6 6l12 12M18 6L6 18" strokeLinecap="round" />
                </svg>
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
