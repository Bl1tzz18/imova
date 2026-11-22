import { redirectTo } from "@/i18n/redirect";
import { ADMIN_HOME } from "@/lib/admin/adminSections";

// /admin — the account menu's "Administrare" entry: straight to the first section.
export default function AdminIndexPage() {
  return redirectTo(ADMIN_HOME);
}
