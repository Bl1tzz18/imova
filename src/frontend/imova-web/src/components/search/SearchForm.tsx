import { useTranslations } from "next-intl";

export function SearchForm() {
  const t = useTranslations("SearchForm");
  const tType = useTranslations("PropertyType");

  return (
    <form
      action="/cauta"
      method="GET"
      className="flex w-full flex-col gap-2 rounded-2xl bg-white p-2 shadow-[var(--shadow-card-hover)] sm:flex-row sm:items-center sm:rounded-full"
    >
      <select
        name="tip"
        defaultValue="vanzare"
        className="h-12 shrink-0 rounded-full bg-transparent px-4 text-sm font-medium text-ink-800 outline-none sm:border-r sm:border-ink-100"
      >
        <option value="vanzare">{t("saleOption")}</option>
        <option value="chirie">{t("rentOption")}</option>
      </select>

      <select
        name="tipProprietate"
        defaultValue=""
        className="h-12 shrink-0 rounded-full bg-transparent px-4 text-sm text-ink-600 outline-none sm:border-r sm:border-ink-100"
      >
        <option value="">{t("anyType")}</option>
        <option value="apartament">{tType("Apartment")}</option>
        <option value="casa">{tType("House")}</option>
        <option value="teren">{tType("Land")}</option>
        <option value="comercial">{tType("Commercial")}</option>
      </select>

      <input
        type="text"
        name="oras"
        placeholder={t("cityPlaceholder")}
        className="h-12 flex-1 rounded-full bg-transparent px-4 text-sm text-ink-900 outline-none placeholder:text-ink-400"
      />

      <button
        type="submit"
        className="flex h-12 shrink-0 items-center justify-center gap-2 rounded-full bg-accent-500 px-6 text-sm font-semibold text-white transition-colors hover:bg-accent-600"
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-4 w-4">
          <circle cx="11" cy="11" r="7" />
          <path d="m20 20-3.5-3.5" strokeLinecap="round" />
        </svg>
        {t("submit")}
      </button>
    </form>
  );
}
