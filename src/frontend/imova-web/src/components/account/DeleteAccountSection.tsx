"use client";

import { useActionState, useState } from "react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { Checkbox } from "@/components/ui/Checkbox";
import { FieldLabel, TextInput } from "@/components/ui/Field";
import {
  deleteAccount,
  requestAccountDeletionLink,
  type AccountDeletionState,
} from "@/lib/account/actions";
import { canConfirmDeletion, deletionItems, type AccountDataSummary } from "@/lib/account/deletion";

const initialState: AccountDeletionState = {};

// Deleting the account: first what it means (what goes, what stays), then — only after "I want to
// delete my account" — the confirmation: the password, or for an account without one (Google
// sign-up) an emailed link. Deleting is immediate and final, so nothing here happens in one click.
export function DeleteAccountSection({
  email,
  hasPassword,
  summary,
}: {
  email: string;
  hasPassword: boolean;
  summary: AccountDataSummary | null;
}) {
  const t = useTranslations("Account");
  const [open, setOpen] = useState(false);
  const [password, setPassword] = useState("");
  const [acknowledged, setAcknowledged] = useState(false);
  const [deleteState, deleteAction, deleting] = useActionState(deleteAccount, initialState);
  const [linkState, linkAction, sendingLink] = useActionState(requestAccountDeletionLink, initialState);

  // React resets the form's fields after each submission; when an answer comes back (e.g. a wrong
  // password), start the confirmation over so the checkbox and the button agree with what's shown.
  const [answered, setAnswered] = useState({ deleteState, linkState });
  if (answered.deleteState !== deleteState || answered.linkState !== linkState) {
    setAnswered({ deleteState, linkState });
    setPassword("");
    setAcknowledged(false);
  }

  const items = summary ? deletionItems(summary) : [];
  const error = hasPassword ? deleteState.error : linkState.error;
  const ready = canConfirmDeletion({ hasPassword, password, acknowledged });

  return (
    <section className="flex flex-col gap-3 border-t border-ink-100 pt-7">
      <h2 className="text-base font-semibold text-red-700">{t("deleteTitle")}</h2>
      <p className="text-sm text-ink-600">{t("deleteIntro")}</p>
      <ul className="list-disc space-y-1 pl-5 text-sm text-ink-600">
        <li>{t("deletion.account")}</li>
        {items.map((item) => (
          <li key={item.key}>{t(`deletion.${item.key}`, { count: item.count })}</li>
        ))}
      </ul>
      <p className="text-sm text-ink-500">{t("deleteKeeps")}</p>
      <p className="text-sm font-medium text-ink-700">{t("deleteDownloadFirst")}</p>

      {!open ? (
        <Button
          type="button"
          variant="secondary"
          onClick={() => setOpen(true)}
          className="mt-1 self-start border-red-200 text-red-700 hover:border-red-300 hover:bg-red-50"
        >
          {t("deleteStart")}
        </Button>
      ) : linkState.linkSent ? (
        <p role="status" className="rounded-xl border border-brand-100 bg-brand-100/60 px-4 py-3 text-sm text-brand-700">
          {t("deleteLinkSent", { email })}
        </p>
      ) : (
        <form
          action={hasPassword ? deleteAction : linkAction}
          className="mt-1 flex flex-col gap-3.5 rounded-2xl border border-red-100 bg-red-50/50 p-5"
        >
          {hasPassword ? (
            <label className="block">
              <FieldLabel>{t("deletePasswordLabel")}</FieldLabel>
              <TextInput
                type="password"
                name="password"
                autoComplete="current-password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </label>
          ) : (
            <p className="text-sm text-ink-600">{t("deleteByEmailHint")}</p>
          )}

          <Checkbox checked={acknowledged} onChange={(e) => setAcknowledged(e.target.checked)} className="text-ink-700">
            {t("deleteAcknowledge")}
          </Checkbox>

          {error && (
            <p role="alert" className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
              {error}
            </p>
          )}

          <div className="flex flex-wrap gap-2.5">
            <Button type="submit" variant="danger" disabled={!ready || deleting || sendingLink}>
              {hasPassword
                ? deleting ? t("deleting") : t("deleteConfirm")
                : sendingLink ? t("sendingLink") : t("deleteSendLink")}
            </Button>
            <Button
              type="button"
              variant="secondary"
              onClick={() => {
                setOpen(false);
                setPassword("");
                setAcknowledged(false);
              }}
            >
              {t("deleteCancel")}
            </Button>
          </div>
        </form>
      )}
    </section>
  );
}
