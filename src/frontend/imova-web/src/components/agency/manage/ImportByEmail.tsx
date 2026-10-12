import { getTranslations } from "next-intl/server";
import { buttonClassName } from "@/components/ui/Button";
import { IMPORT_CONTACT_EMAIL, importContactMailto } from "@/lib/agency/importContact";

// "Importă anunțurile tale" on the agency's Anunțuri tab: importing is a service IMOVA does by hand, so
// the agency only writes to us (a mailto: link) — nothing to upload or fill in here.
export async function ImportByEmail({ agencyName }: { agencyName: string }) {
  const t = await getTranslations("AgencyImport.byEmail");
  const mailto = importContactMailto(t("subject", { name: agencyName }));

  return (
    <section aria-labelledby="import-by-email-title" className="mt-8 rounded-2xl border border-line bg-white p-4 sm:p-6">
      <h2 id="import-by-email-title" className="text-base font-semibold text-ink-950">
        {t("title")}
      </h2>
      <p className="mt-1 max-w-2xl text-sm text-ink-600">{t("intro")}</p>

      <ol className="mt-4 grid gap-3 sm:grid-cols-3">
        {(["step1", "step2", "step3"] as const).map((step, index) => (
          <li key={step} className="flex gap-3 rounded-xl bg-ink-50/60 p-3">
            <span
              aria-hidden
              className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-brand-50 text-sm font-semibold text-brand-700"
            >
              {index + 1}
            </span>
            <span className="min-w-0 text-sm text-ink-700">
              {step === "step1"
                ? t.rich("step1", {
                    email: () => (
                      <a href={mailto} className="break-words font-medium text-brand-700 hover:underline">
                        {IMPORT_CONTACT_EMAIL}
                      </a>
                    ),
                  })
                : t(step)}
            </span>
          </li>
        ))}
      </ol>

      <div className="mt-5">
        {/* A plain <a>: next/link has nothing to do with a mailto: address. */}
        <a href={mailto} className={buttonClassName()}>
          {t("button")}
        </a>
        <p className="mt-2 text-xs text-ink-500">{t("note")}</p>
      </div>
    </section>
  );
}
