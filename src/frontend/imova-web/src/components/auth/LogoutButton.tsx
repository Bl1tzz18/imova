import { logout } from "@/lib/auth/actions";

// A form bound to a Server Action, not a client onClick handler — no client JS needed for this.
export function LogoutButton({ children }: { children: React.ReactNode }) {
  return (
    <form action={logout}>
      <button type="submit" className="text-sm font-medium text-ink-600 transition-colors hover:text-ink-950">
        {children}
      </button>
    </form>
  );
}
