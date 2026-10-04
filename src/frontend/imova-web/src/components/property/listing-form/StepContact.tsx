"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Avatar } from "@/components/ui/Avatar";
import { Checkbox } from "@/components/ui/Checkbox";
import { FieldLabel, SelectInput, TextInput } from "@/components/ui/Field";
import { PhoneInput } from "@/components/ui/PhoneInput";
import {
  CALL_HOUR_SLOTS,
  CONTACT_METHODS,
  MESSAGING_APPS,
  callHourEndSlots,
  callsAllowed,
  contactDefaults,
  contactInputName,
  effectiveContactMethod,
  type ContactMethod,
  type ContactPersonType,
} from "@/lib/property/contact";
import {
  initialPublishAs,
  publishAsOptions,
  readRememberedPublishAs,
  rememberPublishAs,
} from "@/lib/agency/publishAs";
import { cn } from "@/lib/utils/cn";
import type { MyAgency } from "@/types/agency";
import type { Listing, Publisher } from "@/types/listing";
import { DealTypeTabs } from "./DealTypeTabs";

// Step 5: who visitors should contact and how. "Eu" (Self) is the listing's publisher — their
// name/email come from the account, only the phone is asked (it may differ from the account's);
// "Altă persoană" (Other) asks for someone else's name, phone and optional email.
//
// It starts with "Publică ca": private or one of the user's agencies (field `agencyId`, "" for
// private) — only for someone with an active agency, and when editing only for the listing's author.
// When it isn't shown, a hidden field keeps the listing where it is.
export function StepContact({
  listing,
  publishers,
  agencies = [],
  canChangeAgency = true,
  requestedAgencyId,
}: {
  listing?: Listing;
  // Only passed in create mode — a listing's publisher can't be changed afterwards.
  publishers: Publisher[];
  agencies?: MyAgency[];
  canChangeAgency?: boolean;
  // ?agencyId= on the create page ("Adaugă anunț" from an agency).
  requestedAgencyId?: string | null;
}) {
  const t = useTranslations("PropertyForm");
  const tMethod = useTranslations("ContactMethod");

  const editing = listing !== undefined;
  const currentAgency = listing?.agency
    ? { id: listing.agency.id, name: listing.agency.name, logoUrl: listing.agency.logoUrl, isVerified: listing.agency.isVerified }
    : null;
  const agencyOptions = publishAsOptions(agencies, currentAgency);
  const [publishAs, setPublishAs] = useState(() =>
    initialPublishAs({
      options: agencyOptions,
      editingAgencyId: editing ? (listing.agency?.id ?? null) : undefined,
      requested: requestedAgencyId,
    }),
  );
  // The choice remembered in this browser, once mounted (localStorage doesn't exist on the server).
  // Only on mount: later changes are the user's own.
  useEffect(() => {
    if (editing || requestedAgencyId) return;
    const remembered = readRememberedPublishAs();
    if (remembered !== null) setPublishAs(initialPublishAs({ options: agencyOptions, remembered }));
  }, []);
  const showPicker = canChangeAgency && agencyOptions.length > 0;

  const publisher = publishers[0];
  const defaults = contactDefaults(listing, publisher);

  const [personType, setPersonType] = useState<ContactPersonType>(defaults.personType);
  const [hidePhoneNumber, setHidePhoneNumber] = useState(defaults.hidePhoneNumber);
  const [method, setMethod] = useState<ContactMethod>(defaults.preferredContactMethod);
  const shownMethod = effectiveContactMethod(hidePhoneNumber, method);
  // Picked from two dropdowns; a start at/after the chosen end clears the end.
  const [callFrom, setCallFrom] = useState(defaults.callHoursFrom);
  const [callTo, setCallTo] = useState(defaults.callHoursTo);
  const isSelf = personType === "Self";

  return (
    <div>
      <h2 className="font-hero text-xl font-bold text-ink-950">{t("step5Heading")}</h2>

      {showPicker ? (
        <fieldset className="mt-5">
          <legend className="font-hero text-base font-bold text-ink-950">{t("publishAsLabel")}</legend>
          <div className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
            {[{ id: "", name: t("publisherIndividual"), logoUrl: null, isVerified: false }, ...agencyOptions].map((option) => (
              <label
                key={option.id || "private"}
                className={cn(
                  "flex min-h-14 cursor-pointer items-center gap-3 rounded-xl border border-ink-200 px-4 py-3 text-sm",
                  "has-[:checked]:border-brand-500 has-[:checked]:bg-brand-50 has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-brand-500",
                )}
              >
                <input
                  type="radio"
                  name="agencyId"
                  value={option.id}
                  checked={publishAs === option.id}
                  onChange={() => {
                    setPublishAs(option.id);
                    if (!editing) rememberPublishAs(option.id);
                  }}
                  className="shrink-0"
                />
                {option.id && (
                  <Avatar userId={option.id} displayName={option.name} pictureUrl={option.logoUrl} size={32} className="shrink-0" />
                )}
                <span className="min-w-0">
                  <span className="block font-medium text-ink-900 [overflow-wrap:anywhere]">{option.name}</span>
                  {option.id && <span className="block text-xs text-ink-500">{t("publisherAgency")}</span>}
                </span>
              </label>
            ))}
          </div>
        </fieldset>
      ) : (
        <input type="hidden" name="agencyId" value={publishAs} />
      )}

      <div className="mt-5">
        <FieldLabel>{t("contactPersonLabel")}</FieldLabel>
        <DealTypeTabs
          name={contactInputName("personType")}
          value={personType}
          onChange={(value) => setPersonType(value === "Other" ? "Other" : "Self")}
          options={[
            { value: "Self", label: t("contactSelf") },
            { value: "Other", label: t("contactOther") },
          ]}
        />
      </div>

      {/* Keyed by person type: Self and Other each start from their own phone. */}
      <div key={personType} className="mt-5 grid grid-cols-1 gap-4 sm:grid-cols-2">
        {isSelf ? (
          <div className="rounded-xl border border-ink-100 bg-ink-50 px-4 py-3 text-sm sm:col-span-2">
            <p className="font-medium text-ink-900">{defaults.selfName}</p>
            {defaults.selfEmail && <p className="text-ink-600">{defaults.selfEmail}</p>}
            <p className="mt-1 text-xs text-ink-500">{t("contactSelfHint")}</p>
          </div>
        ) : (
          <label className="block">
            <FieldLabel required>{t("contactNameLabel")}</FieldLabel>
            <TextInput name={contactInputName("name")} required maxLength={100} defaultValue={defaults.otherName} />
          </label>
        )}

        <label className="block">
          <FieldLabel required>{t("contactPhoneLabel")}</FieldLabel>
          <PhoneInput
            name={contactInputName("phone")}
            required
            defaultValue={(isSelf ? defaults.selfPhone : defaults.otherPhone) || undefined}
            invalidMessage={t("invalidPhone")}
          />
          {isSelf && <span className="mt-1 block text-xs text-ink-500">{t("contactPhoneHint")}</span>}
        </label>

        {!isSelf && (
          // In-app messages have to go to an account — the publisher's, since this person has none.
          <p className="rounded-xl border border-accent-100 bg-accent-100/40 px-3.5 py-3 text-xs text-ink-700 sm:col-span-2">
            {t("otherContactMessagesNotice")}
          </p>
        )}

        {!isSelf && (
          <label className="block">
            <FieldLabel>{t("contactEmailLabel")}</FieldLabel>
            <TextInput
              name={contactInputName("email")}
              type="email"
              maxLength={254}
              defaultValue={defaults.otherEmail}
            />
          </label>
        )}
      </div>

      <Checkbox
        name={contactInputName("hidePhoneNumber")}
        value="true"
        checked={hidePhoneNumber}
        onChange={(e) => setHidePhoneNumber(e.target.checked)}
        className="mt-5 text-ink-700"
      >
        <span className="block">{t("hidePhoneLabel")}</span>
        <span className="block text-xs text-ink-500">{t("hidePhoneHint")}</span>
      </Checkbox>

      {/* Apps on a hidden number are pointless — the whole group is disabled (and not submitted). */}
      <fieldset disabled={hidePhoneNumber} className={cn("mt-5", hidePhoneNumber && "opacity-50")}>
        <legend className="mb-2 text-sm font-medium text-ink-700">{t("messagingAppsLabel")}</legend>
        <div className="flex flex-wrap gap-x-6 gap-y-2">
          {MESSAGING_APPS.map((app) => (
            <Checkbox
              key={app}
              name={contactInputName("messagingApps")}
              value={app}
              defaultChecked={defaults.messagingApps.includes(app)}
              className="text-ink-700"
            >
              {app}
            </Checkbox>
          ))}
        </div>
      </fieldset>

      <div className="mt-5 grid grid-cols-1 gap-4 sm:grid-cols-2">
        <label className="block">
          <FieldLabel>{t("preferredContactMethodLabel")}</FieldLabel>
          {/* Forced to platform messages while the number is hidden (the backend requires it). */}
          <SelectInput
            name={contactInputName("preferredContactMethod")}
            value={shownMethod}
            disabled={hidePhoneNumber}
            onChange={(e) => setMethod(e.target.value as ContactMethod)}
          >
            {CONTACT_METHODS.map((m) => (
              <option key={m} value={m}>
                {tMethod(m)}
              </option>
            ))}
          </SelectInput>
        </label>

        {callsAllowed(hidePhoneNumber, method) && (
          // Optional, but once one end is picked the other is required.
          <fieldset>
            <legend className="mb-1.5 text-sm font-medium text-ink-700">{t("callHoursLabel")}</legend>
            <div className="flex items-center gap-2">
              <SelectInput
                name={contactInputName("callHoursFrom")}
                aria-label={t("callHoursFrom")}
                value={callFrom}
                required={callTo !== ""}
                onChange={(e) => {
                  setCallFrom(e.target.value);
                  if (e.target.value && callTo && callTo <= e.target.value) setCallTo("");
                }}
              >
                <option value="">{t("callHoursFrom")}</option>
                {CALL_HOUR_SLOTS.slice(0, -1).map((slot) => (
                  <option key={slot} value={slot}>
                    {slot}
                  </option>
                ))}
              </SelectInput>
              <span className="text-ink-400">–</span>
              <SelectInput
                name={contactInputName("callHoursTo")}
                aria-label={t("callHoursTo")}
                value={callTo}
                required={callFrom !== ""}
                onChange={(e) => setCallTo(e.target.value)}
              >
                <option value="">{t("callHoursTo")}</option>
                {callHourEndSlots(callFrom).map((slot) => (
                  <option key={slot} value={slot}>
                    {slot}
                  </option>
                ))}
              </SelectInput>
            </div>
          </fieldset>
        )}
      </div>

      {/* Only relevant before a listing has ever been reviewed — editing an existing listing
          doesn't re-explain the review flow to an owner who already went through it once. */}
      {!listing && (
        <div className="mt-6 flex items-center gap-2.5 rounded-xl border border-accent-100 bg-accent-100/40 px-4 py-3.5">
          <svg
            viewBox="0 0 24 24"
            className="h-[17px] w-[17px] shrink-0 text-accent-600"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.6"
          >
            <path d="M12 3.5l7 2.6v5.2c0 5-3 8-7 9.2-4-1.2-7-4.2-7-9.2V6.1l7-2.6Z" strokeLinejoin="round" />
          </svg>
          <span className="text-[13px] text-ink-900">{t("draftNotice")}</span>
        </div>
      )}
    </div>
  );
}
