"use client";

import { useEffect, useId, useRef, type ReactNode } from "react";
import { Button } from "@/components/ui/Button";

// "Are you sure?" as a native <dialog> (focus stays inside, Escape and a click outside cancel), the
// same look as the listing report dialog. The parent owns `open`; `children` is the explanation and
// any choice to make (e.g. who takes over the listings). Cancelling is impossible while `pending`.
export function ConfirmDialog({
  open,
  title,
  children,
  confirmLabel,
  cancelLabel,
  pendingLabel,
  pending = false,
  danger = false,
  error,
  onConfirm,
  onCancel,
}: {
  open: boolean;
  title: string;
  children?: ReactNode;
  confirmLabel: string;
  cancelLabel: string;
  pendingLabel?: string;
  pending?: boolean;
  danger?: boolean;
  error?: string | null;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  const dialog = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  useEffect(() => {
    const element = dialog.current;
    if (!element) return;
    if (open && !element.open) element.showModal();
    if (!open && element.open) element.close();
  }, [open]);

  function cancel() {
    if (!pending) onCancel();
  }

  return (
    <dialog
      ref={dialog}
      aria-labelledby={titleId}
      onCancel={(e) => {
        e.preventDefault();
        cancel();
      }}
      onClick={(e) => e.target === dialog.current && cancel()}
      className="m-auto w-[calc(100%-2rem)] max-w-md rounded-2xl bg-white p-0 text-left shadow-xl backdrop:bg-ink-950/50"
    >
      <div className="p-6">
        <h2 id={titleId} className="font-display text-lg font-medium text-ink-950">
          {title}
        </h2>
        {children && <div className="mt-2 text-sm text-ink-600">{children}</div>}

        {error && (
          <p role="alert" className="mt-3 text-sm text-red-600">
            {error}
          </p>
        )}

        <div className="mt-5 flex flex-wrap justify-end gap-2">
          <Button type="button" variant="secondary" size="sm" onClick={cancel} disabled={pending}>
            {cancelLabel}
          </Button>
          <Button type="button" variant={danger ? "danger" : "primary"} size="sm" onClick={onConfirm} disabled={pending}>
            {pending && pendingLabel ? pendingLabel : confirmLabel}
          </Button>
        </div>
      </div>
    </dialog>
  );
}
