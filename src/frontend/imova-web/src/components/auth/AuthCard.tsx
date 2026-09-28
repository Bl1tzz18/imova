import type { ReactNode } from "react";

// The centred card every auth page sits in (sign in/up, complete profile, forgot/reset password,
// email confirmation). `header` goes above the title (the login/register tabs), `footer` below the card.
export function AuthCard({
  title,
  subtitle,
  header,
  footer,
  children,
}: {
  title: string;
  subtitle?: ReactNode;
  header?: ReactNode;
  footer?: ReactNode;
  children?: ReactNode;
}) {
  return (
    <main className="relative flex min-h-[calc(100vh-5rem)] flex-col items-center overflow-hidden px-4 py-16 sm:px-6">
      <div className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-[480px] bg-[radial-gradient(55%_55%_at_50%_0%,var(--color-brand-100),transparent_70%)]" />

      <div className="w-full max-w-[400px] rounded-2xl border border-ink-100 bg-white p-8 shadow-[var(--shadow-card)]">
        {header}
        <h1 className="font-display text-2xl font-medium text-ink-950">{title}</h1>
        {subtitle && <p className="mt-1.5 text-sm text-ink-500">{subtitle}</p>}
        {children && <div className="mt-6">{children}</div>}
      </div>

      {footer}
    </main>
  );
}

export function AuthNotice({ tone, children }: { tone: "success" | "error"; children: ReactNode }) {
  return (
    <p
      role={tone === "error" ? "alert" : "status"}
      className={
        tone === "success"
          ? "rounded-xl border border-brand-100 bg-brand-50 px-4 py-3 text-sm text-brand-800"
          : "rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700"
      }
    >
      {children}
    </p>
  );
}
