"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { confirmMediaUpload, requestUploadUrl, uploadFileToBlob } from "@/lib/api/media";

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
  file: File;
  previewUrl: string;
  status: "uploading" | "done" | "error";
  error?: string;
};

// Uploads each selected image straight to Blob Storage via a short-lived SAS URL (see
// lib/api/media.ts), then tells the backend to confirm+validate it. Nothing here needs to
// re-attach the results to the form on submit — ConfirmMediaUpload already persisted each media
// row under `propertyId`, and CreateProperty (actions.ts) submits that same client-generated id,
// so the rows line up automatically once the property itself is created.
export function ImageUploader({ propertyId }: { propertyId: string }) {
  const [items, setItems] = useState<UploadItem[]>([]);
  const t = useTranslations("PropertyForm");

  const handleFiles = (files: FileList | null) => {
    if (!files) return;

    for (const file of Array.from(files)) {
      const extension = getAllowedExtension(file.name);
      const id = crypto.randomUUID();

      if (!extension) {
        setItems((prev) => [
          ...prev,
          { id, file, previewUrl: "", status: "error", error: t("photoInvalidType") },
        ]);
        continue;
      }

      if (file.size > MAX_FILE_SIZE_BYTES) {
        setItems((prev) => [
          ...prev,
          { id, file, previewUrl: URL.createObjectURL(file), status: "error", error: t("photoTooLarge") },
        ]);
        continue;
      }

      const previewUrl = URL.createObjectURL(file);
      setItems((prev) => [...prev, { id, file, previewUrl, status: "uploading" }]);

      void (async () => {
        try {
          const { uploadUrl, blobName } = await requestUploadUrl(propertyId, extension);
          await uploadFileToBlob(uploadUrl, file);
          await confirmMediaUpload(propertyId, blobName);
          setItems((prev) => prev.map((it) => (it.id === id ? { ...it, status: "done" } : it)));
        } catch {
          setItems((prev) =>
            prev.map((it) => (it.id === id ? { ...it, status: "error", error: t("photoUploadFailed") } : it))
          );
        }
      })();
    }
  };

  const removeItem = (id: string) => {
    setItems((prev) => prev.filter((it) => it.id !== id));
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
            <div key={item.id} className="group relative aspect-square overflow-hidden rounded-xl border border-ink-100 bg-ink-100">
              {item.previewUrl && (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={item.previewUrl} alt="" className="h-full w-full object-cover" />
              )}

              {item.status === "uploading" && (
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
                onClick={() => removeItem(item.id)}
                aria-label={t("removePhoto")}
                className="absolute right-1.5 top-1.5 flex h-6 w-6 items-center justify-center rounded-full bg-black/60 text-white opacity-0 transition-opacity group-hover:opacity-100"
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
