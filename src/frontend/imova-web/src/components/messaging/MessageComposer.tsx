"use client";

import { useLayoutEffect, useRef, useState, type KeyboardEvent } from "react";
import { useTranslations } from "next-intl";
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
  const textArea = useRef<HTMLTextAreaElement>(null);
  const lastTypingSent = useRef<number | null>(null);

  // The pill grows with the text (up to max-h-36, then scrolls).
  useLayoutEffect(() => {
    const el = textArea.current;
    if (!el) return;
    el.style.height = "auto";
    el.style.height = `${el.scrollHeight}px`;
  }, [body]);

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
    <div>
      {images.length > 0 && (
        <ul className="mb-2 flex flex-wrap gap-2">
          {images.map((image) => (
            <li key={image.key} className="relative h-16 w-16 overflow-hidden rounded-[14px] border border-line">
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

      <div className="flex items-end gap-2">
        <button
          type="button"
          onClick={() => fileInput.current?.click()}
          disabled={disabled || images.length >= MAX_ATTACHMENTS}
          aria-label={t("attachImages", { count: images.length, max: MAX_ATTACHMENTS })}
          title={t("attachImages", { count: images.length, max: MAX_ATTACHMENTS })}
          className="flex h-11 shrink-0 items-center gap-1.5 rounded-full px-3 text-xs font-medium text-ink-500 transition-colors hover:bg-bubble hover:text-ink-900 disabled:opacity-40"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" className="h-5 w-5" aria-hidden>
            <path d="M4 5h16v14H4zM4 15l4-4 4 4 3-3 5 5M15 9h.01" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
          {images.length}/{MAX_ATTACHMENTS}
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

        <div className="flex min-h-11 flex-1 items-center rounded-[22px] bg-bubble px-4 py-2.5">
          <textarea
            ref={textArea}
            value={body}
            onChange={(e) => handleChange(e.target.value)}
            onKeyDown={handleKeyDown}
            maxLength={MAX_MESSAGE_LENGTH}
            rows={1}
            disabled={disabled}
            autoFocus={autoFocus}
            placeholder={placeholder ?? t("composerPlaceholder")}
            aria-label={placeholder ?? t("composerPlaceholder")}
            className="max-h-36 w-full resize-none bg-transparent text-sm leading-5 text-ink-900 outline-none placeholder:text-ink-400"
          />
        </div>

        <button
          type="button"
          onClick={() => void send()}
          disabled={!canSend}
          aria-label={sending ? t("sending") : t("send")}
          title={t("send")}
          className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-accent-500 text-white shadow-sm shadow-accent-500/30 transition-colors hover:bg-accent-600 disabled:opacity-40"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-5 w-5" aria-hidden>
            <path d="M21 3 10 14M21 3l-7 18-4-7-7-4 18-7Z" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </button>
      </div>

      <div className="mt-1 flex justify-between gap-3 px-1 text-xs">
        {error ? <p className="text-accent-700">{error}</p> : <span />}
        {body.length > MAX_MESSAGE_LENGTH - 200 && (
          <span className="text-ink-500">
            {body.length}/{MAX_MESSAGE_LENGTH}
          </span>
        )}
      </div>
    </div>
  );
}
