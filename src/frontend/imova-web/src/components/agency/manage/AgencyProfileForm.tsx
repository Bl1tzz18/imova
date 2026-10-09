"use client";

import { startTransition, useActionState, useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { FieldLabel, SelectInput, TextAreaInput, TextInput } from "@/components/ui/Field";
import { PhoneInput } from "@/components/ui/PhoneInput";
import { AgencyPreviewCard } from "@/components/agency/manage/AgencyPreviewCard";
import { useUnsavedChangesWarning } from "@/components/agency/manage/useUnsavedChangesWarning";
import { createAgency, updateAgency, type AgencyProfileState } from "@/lib/agency/manageActions";
import {
  AGENCY_ADDRESS_MAX,
  AGENCY_BIO_MAX,
  AGENCY_EMAIL_MAX,
  AGENCY_NAME_MAX,
  AGENCY_WEBSITE_MAX,
  agencyProfileChanged,
  bioCounter,
  type AgencyProfileValues,
} from "@/lib/agency/manage";
import { cn } from "@/lib/utils/cn";

const initialState: AgencyProfileState = {};

export type RaionOption = { id: string; label: string };

// The agency's profile — creating one (/account/agencies/new) or editing it (its Profil tab). Beside
// it (under it on a phone) a preview of how the agency shows on IMOVA, following what's typed. When
// editing, Save stays off until something changed; leaving with unsaved changes asks first.
export function AgencyProfileForm({
  mode,
  agencyId,
  initial,
  raioane,
  accountEmail,
  logoUrl = null,
  isVerified = false,
}: {
  mode: "create" | "edit";
  agencyId?: string;
  initial: AgencyProfileValues;
  raioane: RaionOption[];
  // Create: the email used when the field is left empty.
  accountEmail?: string;
  logoUrl?: string | null;
  isVerified?: boolean;
}) {
  const t = useTranslations("AgencyManage");
  const [state, formAction, pending] = useActionState(mode === "create" ? createAgency : updateAgency, initialState);
  const form = useRef<HTMLFormElement>(null);
  const [values, setValues] = useState(initial);
  const [saved, setSaved] = useState(initial);
  const [lastSavedAt, setLastSavedAt] = useState<number | undefined>(undefined);

  // A save went through: what's on screen is now what's saved.
  if (state.savedAt && state.savedAt !== lastSavedAt) {
    setLastSavedAt(state.savedAt);
    setSaved(values);
  }

  const dirty = agencyProfileChanged(saved, values);
  // Not while the form is on its way (creating redirects when it's done).
  useUnsavedChangesWarning(dirty && !pending, t("unsavedChanges"));

  // The phone field keeps its own state and writes a hidden input (a country is picked with a
  // click) — read it once React has.
  function readPhone() {
    setTimeout(() => {
      if (!form.current) return;
      const phone = String(new FormData(form.current).get("phone") ?? "");
      setValues((v) => (v.phone === phone ? v : { ...v, phone }));
    }, 0);
  }

  // The saved phone as the field writes it, so the form doesn't start out "changed".
  useEffect(() => {
    if (!form.current) return;
    const phone = String(new FormData(form.current).get("phone") ?? "");
    setValues((v) => ({ ...v, phone }));
    setSaved((s) => (agencyProfileChanged({ ...s, phone }, s) ? s : { ...s, phone }));
  }, []);

  function set<K extends keyof AgencyProfileValues>(field: K) {
    return (e: { target: { value: string } }) => setValues((v) => ({ ...v, [field]: e.target.value }));
  }

  const bio = bioCounter(values.bio);
  const raionName = raioane.find((r) => r.id === values.raionId)?.label ?? null;
  const showSaved = state.savedAt !== undefined && state.savedAt === lastSavedAt && !dirty;

  return (
    <div className="flex flex-col gap-6 lg:flex-row lg:items-start">
      <form
        ref={form}
        onSubmit={(e) => {
          // Not `action={formAction}`: React resets a form after such a submit, and the native reset
          // would put the (controlled) city back to its first option while the state keeps the saved one.
          e.preventDefault();
          const data = new FormData(e.currentTarget);
          startTransition(() => formAction(data));
        }}
        onInput={readPhone}
        onClick={readPhone}
        className="flex min-w-0 flex-1 flex-col gap-4"
      >
        {agencyId && <input type="hidden" name="agencyId" value={agencyId} />}

        <label className="block">
          <FieldLabel required>{t("nameLabel")}</FieldLabel>
          <TextInput name="name" value={values.name} onChange={set("name")} required maxLength={AGENCY_NAME_MAX} autoComplete="organization" />
        </label>

        {/* A group, not a <label>: the field holds a button (the country), which a label would
            click and take its name from. */}
        <div role="group" aria-labelledby="agency-phone-label" aria-describedby="agency-phone-hint">
          <span id="agency-phone-label">
            <FieldLabel required>{t("phoneLabel")}</FieldLabel>
          </span>
          <PhoneInput name="phone" defaultValue={initial.phone || undefined} required invalidMessage={t("phoneInvalid")} />
          <span id="agency-phone-hint" className="mt-1 block text-xs text-ink-500">
            {t("phoneHint")}
          </span>
        </div>

        <label className="block">
          <FieldLabel required={mode === "edit"}>{t("emailLabel")}</FieldLabel>
          <TextInput
            type="email"
            name="email"
            value={values.email}
            onChange={set("email")}
            required={mode === "edit"}
            maxLength={AGENCY_EMAIL_MAX}
            placeholder={accountEmail}
            autoComplete="email"
          />
          <span className="mt-1 block text-xs text-ink-500">{mode === "create" ? t("emailHintCreate") : t("emailHint")}</span>
        </label>

        <label className="block">
          <FieldLabel>{t("websiteLabel")}</FieldLabel>
          <TextInput
            type="url"
            name="website"
            value={values.website}
            onChange={set("website")}
            maxLength={AGENCY_WEBSITE_MAX}
            placeholder="https://"
            autoComplete="url"
          />
        </label>

        <div className="grid gap-4 sm:grid-cols-2">
          <label className="block">
            <FieldLabel>{t("cityLabel")}</FieldLabel>
            <SelectInput name="raionId" value={values.raionId} onChange={set("raionId")}>
              <option value="">{t("cityNone")}</option>
              {raioane.map((raion) => (
                <option key={raion.id} value={raion.id}>
                  {raion.label}
                </option>
              ))}
            </SelectInput>
          </label>

          <label className="block">
            <FieldLabel>{t("addressLabel")}</FieldLabel>
            <TextInput
              name="address"
              value={values.address}
              onChange={set("address")}
              maxLength={AGENCY_ADDRESS_MAX}
              placeholder={t("addressPlaceholder")}
              autoComplete="street-address"
            />
          </label>
        </div>

        <label className="block">
          <FieldLabel>{t("bioLabel")}</FieldLabel>
          <TextAreaInput
            name="bio"
            value={values.bio}
            onChange={set("bio")}
            rows={6}
            maxLength={AGENCY_BIO_MAX}
            placeholder={t("bioPlaceholder")}
            aria-describedby="agency-bio-counter"
          />
          <span
            id="agency-bio-counter"
            className={cn("mt-1 block text-right text-xs tabular-nums", bio.nearLimit ? "text-accent-700" : "text-ink-400")}
          >
            {t("bioCounter", { length: bio.length, max: bio.max })}
          </span>
        </label>

        {state.error && (
          <p role="alert" className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
            {state.error}
          </p>
        )}
        {showSaved && (
          <p role="status" className="rounded-xl border border-brand-100 bg-brand-100/60 px-4 py-3 text-sm text-brand-700">
            {t("saved")}
          </p>
        )}

        <div className="flex flex-wrap items-center gap-3">
          <Button type="submit" disabled={pending || (mode === "edit" && !dirty)}>
            {pending ? t("saving") : mode === "create" ? t("createSubmit") : t("save")}
          </Button>
          {mode === "edit" && dirty && !pending && <span className="text-sm text-ink-500">{t("unsavedHint")}</span>}
        </div>
      </form>

      <aside className="lg:sticky lg:top-24 lg:w-72 lg:shrink-0">
        <p className="mb-2 text-xs font-medium uppercase tracking-wide text-ink-500">{t("previewTitle")}</p>
        <AgencyPreviewCard
          id={agencyId ?? "new"}
          name={values.name}
          logoUrl={logoUrl}
          isVerified={isVerified}
          city={raionName}
          phone={values.phone}
          email={values.email || accountEmail || ""}
          website={values.website}
        />
      </aside>
    </div>
  );
}
