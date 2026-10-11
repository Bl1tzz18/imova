"use client";

import { useId, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { importFileProblem, importJobHref } from "@/lib/agency/importJobs";

// Choose a CSV and upload it; the import then runs in the background and the page moves to its
// result. The file is checked here first (name, size), again by the web app's route and the API.
export function ImportUploadForm({ agencyId, disabled }: { agencyId: string; disabled: boolean }) {
  const t = useTranslations("AgencyImport.page");
  const router = useRouter();
  const inputId = useId();
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function choose(chosen: File | null) {
    setError(null);
    setFile(chosen);
    if (!chosen) return;
    const problem = importFileProblem(chosen);
    if (problem === "notCsv") setError(t("fileNotCsv"));
    if (problem === "empty") setError(t("fileEmpty"));
    if (problem === "tooLarge") setError(t("fileTooLarge"));
  }

  function upload(event: React.FormEvent) {
    event.preventDefault();
    if (!file || importFileProblem(file)) return;

    startTransition(async () => {
      const body = new FormData();
      body.append("file", file, file.name);
      const res = await fetch(`/account/agencies/${agencyId}/import/upload`, { method: "POST", body });
      const json = (await res.json().catch(() => null)) as { id?: string; error?: string } | null;
      if (!res.ok || !json?.id) {
        setError(json?.error ?? t("failedWhole"));
        return;
      }

      router.push(importJobHref(agencyId, json.id));
    });
  }

  const locked = disabled || pending;
  return (
    <form onSubmit={upload} className="space-y-3">
      <div className="flex flex-wrap items-center gap-3">
        <input
          id={inputId}
          type="file"
          accept=".csv,text/csv"
          className="peer sr-only"
          disabled={locked}
          onChange={(e) => choose(e.target.files?.[0] ?? null)}
        />
        <label
          htmlFor={inputId}
          className={
            locked
              ? "inline-flex h-11 cursor-not-allowed items-center rounded-full border border-ink-200 bg-white px-5 text-sm font-medium text-ink-400"
              : "inline-flex h-11 cursor-pointer items-center rounded-full border border-ink-200 bg-white px-5 text-sm font-medium text-ink-900 hover:border-ink-300 hover:bg-ink-50 peer-focus-visible:ring-2 peer-focus-visible:ring-brand-600"
          }
        >
          {t("chooseFile")}
        </label>
        <span className="min-w-0 truncate text-sm text-ink-600">{file ? file.name : t("noFile")}</span>
      </div>

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}

      <Button type="submit" disabled={locked || !file || !!error}>
        {pending ? t("uploading") : t("upload")}
      </Button>
    </form>
  );
}
