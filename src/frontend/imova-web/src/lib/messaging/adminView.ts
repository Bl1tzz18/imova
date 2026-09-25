// The admin messaging page's two tabs: what still needs attention, and what's been dealt with.
export type AdminView = "active" | "resolved";

export function parseAdminView(param: string | string[] | undefined): AdminView {
  return param === "resolved" ? "resolved" : "active";
}

export function adminViewHref(view: AdminView): string {
  return view === "resolved" ? "/admin/messaging?view=resolved" : "/admin/messaging";
}
