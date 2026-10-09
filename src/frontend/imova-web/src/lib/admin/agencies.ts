// /admin/agencies — the address (?q=&show=&page=) and the API query, pure (Vitest-covered).

export const ADMIN_AGENCY_FILTERS = ["all", "unverified", "verified"] as const;
export type AdminAgencyFilter = (typeof ADMIN_AGENCY_FILTERS)[number];

export const ADMIN_AGENCIES_PAGE_SIZE = 20;

export type AdminAgenciesState = { q: string; show: AdminAgencyFilter; page: number };

type Params = Record<string, string | string[] | undefined>;

function first(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

export function parseAdminAgenciesParams(params: Params): AdminAgenciesState {
  const show = first(params.show);
  const page = Number(first(params.page));
  return {
    q: (first(params.q) ?? "").trim(),
    show: (ADMIN_AGENCY_FILTERS as readonly string[]).includes(show ?? "") ? (show as AdminAgencyFilter) : "all",
    page: Number.isInteger(page) && page > 1 ? page : 1,
  };
}

export function adminAgenciesHref(state: Partial<AdminAgenciesState>): string {
  const params = new URLSearchParams();
  if (state.q) params.set("q", state.q);
  if (state.show && state.show !== "all") params.set("show", state.show);
  if (state.page && state.page > 1) params.set("page", String(state.page));
  const query = params.toString();
  return `/admin/agencies${query ? `?${query}` : ""}`;
}

export function adminAgenciesApiQuery(state: AdminAgenciesState, pageSize = ADMIN_AGENCIES_PAGE_SIZE): string {
  const params = new URLSearchParams();
  if (state.q) params.set("q", state.q);
  if (state.show !== "all") params.set("verified", String(state.show === "verified"));
  params.set("page", String(state.page));
  params.set("pageSize", String(pageSize));
  return params.toString();
}
