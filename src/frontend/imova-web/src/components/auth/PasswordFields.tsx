"use client";

import { useId, useState } from "react";
import { useTranslations } from "next-intl";
import { FieldLabel, TextInput } from "@/components/ui/Field";
import { cn } from "@/lib/utils/cn";
import { confirmationState, passwordChecks, PASSWORD_RULES, type PasswordRule } from "@/lib/auth/passwordRules";

const RULE_LABEL_KEYS: Record<PasswordRule, string> = {
  minLength: "passwordRuleMinLength",
  number: "passwordRuleNumber",
  special: "passwordRuleSpecial",
};

// A new password plus its confirmation, for every place a user chooses a password. Under the
// password: the policy as a checklist that ticks off live while typing. Under the confirmation:
// whether the two match, shown as soon as the confirmation is being typed. `attempted` (set by the
// form after a blocked submit) turns whatever is still missing red. The form decides whether to
// submit with meetsPasswordRules / confirmationState on the submitted values.
export function PasswordFields({
  passwordName,
  passwordLabel,
  confirmName = "confirmPassword",
  confirmLabel,
  attempted = false,
}: {
  passwordName: string;
  passwordLabel: string;
  confirmName?: string;
  confirmLabel: string;
  attempted?: boolean;
}) {
  const t = useTranslations("Auth");
  const [password, setPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const rulesId = useId();
  const matchId = useId();

  const checks = passwordChecks(password);
  const allMet = PASSWORD_RULES.every((rule) => checks[rule]);
  const match = confirmationState(password, confirmation);
  const showMatch = match !== "empty" || attempted;

  return (
    <>
      <div>
        <label className="block">
          <FieldLabel>{passwordLabel}</FieldLabel>
          <TextInput
            type="password"
            name={passwordName}
            autoComplete="new-password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            aria-describedby={rulesId}
            aria-invalid={attempted && !allMet ? true : undefined}
          />
        </label>

        <ul id={rulesId} aria-live="polite" className="mt-2 flex flex-col gap-1">
          {PASSWORD_RULES.map((rule) => (
            <RequirementRow key={rule} status={checks[rule] ? "met" : attempted ? "failed" : "pending"}>
              {t(RULE_LABEL_KEYS[rule])}
            </RequirementRow>
          ))}
        </ul>
      </div>

      <div>
        <label className="block">
          <FieldLabel>{confirmLabel}</FieldLabel>
          <TextInput
            type="password"
            name={confirmName}
            autoComplete="new-password"
            required
            value={confirmation}
            onChange={(e) => setConfirmation(e.target.value)}
            aria-describedby={matchId}
            aria-invalid={showMatch && match !== "match" ? true : undefined}
          />
        </label>

        <ul id={matchId} aria-live="polite" className="mt-2">
          {showMatch && (
            <RequirementRow status={match === "match" ? "met" : "failed"}>
              {match === "match" ? t("passwordsMatch") : t("passwordMismatch")}
            </RequirementRow>
          )}
        </ul>
      </div>
    </>
  );
}

function RequirementRow({ status, children }: { status: "met" | "pending" | "failed"; children: React.ReactNode }) {
  return (
    <li
      className={cn(
        "flex items-center gap-2 text-[13px] transition-colors",
        status === "met" && "text-emerald-700",
        status === "pending" && "text-ink-500",
        status === "failed" && "text-accent-700",
      )}
    >
      <span
        aria-hidden
        className={cn(
          "flex h-4 w-4 shrink-0 items-center justify-center rounded-full border transition-colors",
          status === "met" && "border-emerald-600 bg-emerald-600 text-white",
          status === "pending" && "border-ink-300 bg-white",
          status === "failed" && "border-accent-600 bg-accent-600 text-white",
        )}
      >
        {status === "met" && (
          <svg viewBox="0 0 16 16" className="h-2.5 w-2.5" fill="none" stroke="currentColor" strokeWidth="2.4">
            <path d="M3.5 8.5 6.5 11.5 12.5 4.5" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        )}
        {status === "failed" && (
          <svg viewBox="0 0 16 16" className="h-2.5 w-2.5" fill="none" stroke="currentColor" strokeWidth="2.4">
            <path d="M4.5 4.5 11.5 11.5M11.5 4.5 4.5 11.5" strokeLinecap="round" />
          </svg>
        )}
      </span>
      {children}
    </li>
  );
}
