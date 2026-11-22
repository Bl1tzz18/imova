import { redirect } from "next/navigation";
import { ADMIN_HOME } from "@/lib/admin/adminSections";

// /admin — the account menu's "Administrare" entry: straight to the first section.
export default function AdminIndexPage() {
  redirect(ADMIN_HOME);
}
