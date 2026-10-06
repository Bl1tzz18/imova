"use client";

import type { FormEvent, ReactNode } from "react";

// A plain GET form (works without JavaScript — it keeps its submit button) that also submits
// itself as soon as a select or checkbox in it changes. Text fields still wait for Enter or the button.
export function AutoSubmitForm({ action, className, children }: { action: string; className?: string; children: ReactNode }) {
  function onChange(event: FormEvent<HTMLFormElement>) {
    const target = event.target as HTMLInputElement | HTMLSelectElement;
    if (target instanceof HTMLSelectElement || (target instanceof HTMLInputElement && target.type === "checkbox")) {
      event.currentTarget.requestSubmit();
    }
  }

  return (
    <form action={action} method="get" className={className} onChange={onChange}>
      {children}
    </form>
  );
}
