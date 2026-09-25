"use client";

import { useRef, useState, type KeyboardEvent } from "react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { uploadFileToBlob } from "@/lib/api/media";
import { requestAttachmentUploadUrl } from "@/lib/messaging/actions";
import { ATTACHMENT_ACCEPT, attachmentExtension, pickAttachments } from "@/lib/messaging/attachments";
import { MAX_ATTACHMENTS, MAX_MESSAGE_LENGTH, shouldSendTyping } from "@/lib/messaging/thread";
import { cn } from "@/lib/utils/cn";

type PendingImage = { key: string; previewUrl: string; blobName: string | null; failed: boolean };

// Text (Enter sends, Shift+Enter is a new line) plus up to 5 images, each uploaded straight to blob
// storage as soon as it's picked; the message carries their blob names.
export function MessageComposer({
  onSend,
  onTyping,
  disabled = false,
  autoFocus = false,
  placeholder,
}: {
  onSend: (body: string, attachmentBlobNames: string[]) => Promise<{ error?: string }>;
  onTyping?: () => void;
  disabled?: boolean;
  autoFocus?: boolean;
  placeholder?: string;
}) {
  const t = useTranslations("Messages");
  const [body, setBody] = useState("");
  const [images, setImages] = useState<PendingImage[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [sending, setSending] = useState(false);
  const fileInput = useRef<HTMLInputElement>(null);
  const lastTypingSent = useRef<number | null>(null);

  const uploading = images.some((i) => i.blobName === null && !i.failed);
  const ready = images.filter((i) => i.blobName !== null);
  const canSend = !disabled && !sending && !uploading && (body.trim().length > 0 || ready.length > 0);

  async function addFiles(fileList: FileList | null) {
    const { accepted, problems } = pickAttachments(Array.from(fileList ?? []), images.length, MAX_ATTACHMENTS);
    setError(problems.length > 0 ? problems.map((p) => t(`attachmentProblem.${p}`, { max: MAX_ATTACHMENTS })).join(" ") : null);

    const added = accepted.map((file) => ({ file, image: { key: crypto.randomUUID(), previewUrl: URL.createObjectURL(file), blobName: null, failed: false } }));
    setImages((current) => [...current, ...added.map((a) => a.image)]);

    await Promise.all(
      added.map(async ({ file, image }) => {
        const target = await requestAttachmentUploadUrl(attachmentExtension(file)!);
        let blobName: string | null = null;
        if (target.error === undefined) {
          try {
            await uploadFileToBlob(target.uploadUrl, file);
            blobName = target.blobName;
          } catch {
            blobName = null;
          }
        }
        setImages((current) =>
          current.map((i) => (i.key === image.key ? { ...i, blobName, failed: blobName === null } : i)),
        );
      }),
    );
  }

  function removeImage(key: string) {
    setImages((current) => {
      const removed = current.find((i) => i.key === key);
      if (removed) URL.revokeObjectURL(removed.previewUrl);
      return current.filter((i) => i.key !== key);
    });
  }

  async function send() {
    if (!canSend) return;
    setSending(true);
    setError(null);
    const result = await onSend(body.trim(), ready.map((i) => i.blobName!));
    setSending(false);
    if (result.error) {
      setError(result.error);
      return;
    }
    images.forEach((i) => URL.revokeObjectURL(i.previewUrl));
    setBody("");
    setImages([]);
    lastTypingSent.current = null;
  }

  function handleKeyDown(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === "Enter" && !e.shiftKey && !e.nativeEvent.isComposing) {
      e.preventDefault();
      void send();
    }
  }

  function handleChange(value: string) {
    setBody(value);
    const now = Date.now();
    if (onTyping && value.trim() && shouldSendTyping(lastTypingSent.current, now)) {
      lastTypingSent.current = now;
      onTyping();
    }
  }

  return (
    <div className="rounded-2xl border border-ink-200 bg-white p-3">
      {images.length > 0 && (
        <ul className="mb-3 flex flex-wrap gap-2">
          {images.map((image) => (
            <li key={image.key} className="relative h-16 w-16 overflow-hidden rounded-lg border border-ink-100">
              {/* eslint-disable-next-line @next/next/no-img-element -- local object URL preview */}
              <img src={image.previewUrl} alt="" className={cn("h-full w-full object-cover", !image.blobName && "opacity-50")} />
              {image.blobName === null && !image.failed && (
                <span className="absolute inset-0 flex items-center justify-center text-[10px] font-medium text-ink-700">
                  {t("uploading")}
                </span>
              )}
              {image.failed && (
                <span className="absolute inset-0 flex items-center justify-center bg-accent-100/80 text-[10px] font-medium text-accent-700">
                  {t("uploadFailed")}
                </span>
              )}
              <button
                type="button"
                onClick={() => removeImage(image.key)}
                aria-label={t("removeImage")}
                className="absolute right-0.5 top-0.5 flex h-5 w-5 items-center justify-center rounded-full bg-ink-950/70 text-xs text-white"
              >
                ×
              </button>
            </li>
          ))}
        </ul>
      )}

      <textarea
        value={body}
        onChange={(e) => handleChange(e.target.value)}
        onKeyDown={handleKeyDown}
        maxLength={MAX_MESSAGE_LENGTH}
        rows={3}
        disabled={disabled}
        autoFocus={autoFocus}
        placeholder={placeholder ?? t("composerPlaceholder")}
        aria-label={t("composerPlaceholder")}
        className="w-full resize-none bg-transparent text-sm text-ink-900 outline-none placeholder:text-ink-400"
      />

      <div className="mt-2 flex items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => fileInput.current?.click()}
            disabled={disabled || images.length >= MAX_ATTACHMENTS}
            className="flex items-center gap-1.5 rounded-full px-2 py-1 text-sm text-ink-600 transition-colors hover:bg-ink-50 disabled:opacity-40"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" className="h-4 w-4">
              <path d="M4 5h16v14H4zM4 15l4-4 4 4 3-3 5 5M15 9h.01" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            {t("attachImages", { count: images.length, max: MAX_ATTACHMENTS })}
          </button>
          <input
            ref={fileInput}
            type="file"
            accept={ATTACHMENT_ACCEPT}
            multiple
            hidden
            onChange={(e) => {
              void addFiles(e.target.files);
              e.target.value = "";
            }}
          />
          {body.length > MAX_MESSAGE_LENGTH - 200 && (
            <span className="text-xs text-ink-500">
              {body.length}/{MAX_MESSAGE_LENGTH}
            </span>
          )}
        </div>
        <Button type="button" size="sm" onClick={() => void send()} disabled={!canSend}>
          {sending ? t("sending") : t("send")}
        </Button>
      </div>

      {error && <p className="mt-2 text-sm text-accent-700">{error}</p>}
    </div>
  );
}
