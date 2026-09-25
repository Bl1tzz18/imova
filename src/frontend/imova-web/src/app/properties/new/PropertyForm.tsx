"use client";

import { useActionState, useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { StepIndicator, type StepDef } from "@/components/property/listing-form/StepIndicator";
import { StepTypeLocation } from "@/components/property/listing-form/StepTypeLocation";
import { StepDetails } from "@/components/property/listing-form/StepDetails";
import { StepPhotos } from "@/components/property/listing-form/StepPhotos";
import { StepPriceContact } from "@/components/property/listing-form/StepPriceContact";
import { ListingTips } from "@/components/property/listing-form/ListingTips";
import { SuccessPanel } from "@/components/property/listing-form/SuccessPanel";
import { updateListingDetails } from "@/lib/property/actions";
import { createListing, type CreateListingState } from "./actions";
import type { Listing, Publisher } from "@/types/listing";

const initialState: CreateListingState = {};

const STEP_COUNT = 4;

// Reused as-is for both listing creation (no `listing` prop) and editing an existing listing
// (`listing` supplied — see /my-listings/[id]/edit/page.tsx): same steps, same fields, all
// pre-filled and editable in edit mode. One submit carries both the physical Property and the
// Listing offer. Only the parts that genuinely differ between create and edit — which id/action
// is used, the publisher picker, the final button's label, and what happens after a successful
// submit — branch on whether `listing` is present.
export function PropertyForm({ listing, publishers = [] }: { listing?: Listing; publishers?: Publisher[] }) {
  const isEdit = listing != null;
  const boundUpdateAction = isEdit ? updateListingDetails.bind(null, listing.id) : null;
  const [state, formAction, pending] = useActionState(
    isEdit ? boundUpdateAction! : createListing,
    initialState,
  );
  const [step, setStep] = useState(1);
  const location = listing?.property.location;
  const [propertyType, setPropertyType] = useState(listing?.property.propertyType ?? "Apartment");
  const [transactionType, setTransactionType] = useState<string>(listing?.transactionType ?? "Sale");
  const [raionId, setRaionId] = useState(location?.raionId ?? "");
  const [localitateId, setLocalitateId] = useState(location?.localitateId ?? "");
  const [chisinauSectorId, setChisinauSectorId] = useState(location?.chisinauSectorId ?? "");

  // Frozen at mount so it keeps reflecting "this listing was Rejected when the owner opened the
  // edit page" for the whole session, regardless of the automatic background refresh Next.js
  // runs after a successful server action (which would otherwise flip listing.status to
  // PendingReview mid-edit and change the button label/notice out from under the user).
  const [wasRejected] = useState(() => listing?.status === "Rejected");

  // Generated up front (client-side only, in an effect — crypto.randomUUID() during the
  // initial render would produce a different value on the server than on the client and
  // trigger a hydration mismatch) so photos can be uploaded and attached server-side (see
  // ImageUploader/actions.ts) before the listing itself is created. Editing an existing
  // listing already has a real id, so this only runs for create mode.
  const [generatedId, setGeneratedId] = useState<string | null>(null);
  useEffect(() => {
    if (!isEdit) setGeneratedId(crypto.randomUUID());
  }, [isEdit]);
  const listingId = listing?.id ?? generatedId;

  const formRef = useRef<HTMLFormElement>(null);
  const stepRefs = useRef<Array<HTMLDivElement | null>>([]);

  const t = useTranslations("PropertyForm");
  const tEdit = useTranslations("EditListingPage");

  // Only the currently visible step's fields are validated: a hidden field can't show its
  // native validation bubble, so jumping straight to a distant step (skipping ones never
  // rendered visible) is not allowed — advancing one step at a time keeps every check on a
  // field the browser can actually focus and report against.
  function validateCurrentStep(): boolean {
    const container = stepRefs.current[step - 1];
    if (!container) return true;
    const invalid = container.querySelector<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>(
      "input:invalid, select:invalid, textarea:invalid",
    );
    if (invalid) {
      // Fires the element's "invalid" event — a collapsed accordion section (see
      // DetailsAccordion) opens itself in response. A hidden field can't show the browser's
      // validation bubble, so report again once the section has rendered open.
      const wasHidden = invalid.getClientRects().length === 0;
      invalid.reportValidity();
      if (wasHidden) {
        requestAnimationFrame(() =>
          requestAnimationFrame(() => {
            invalid.focus();
            invalid.reportValidity();
          }),
        );
      }
      return false;
    }
    return true;
  }

  // Switching Raion invalidates whatever Localitate/Sector was picked under the previous one —
  // ChisinauSectorId only ever makes sense while Chișinău is selected.
  function handleRaionIdChange(value: string) {
    setRaionId(value);
    setLocalitateId("");
    setChisinauSectorId("");
  }

  function goToStep(target: number) {
    if (target <= step) {
      setStep(target);
      return;
    }
    if (target === step + 1 && validateCurrentStep()) {
      setStep(target);
    }
  }

  function handlePrimaryClick() {
    if (step < STEP_COUNT) {
      goToStep(step + 1);
    } else if (validateCurrentStep()) {
      formRef.current?.requestSubmit();
    }
  }

  const steps: StepDef[] = [
    { number: 1, label: t("step1Label") },
    { number: 2, label: t("step2Label") },
    { number: 3, label: t("step3Label") },
    { number: 4, label: t("step4Label") },
  ].map((s) => ({
    ...s,
    state: s.number < step ? "done" : s.number === step ? "active" : "upcoming",
    clickable: s.number <= step + 1,
  }));

  if (state.success && !isEdit) {
    return <SuccessPanel />;
  }

  const primaryLabel = pending
    ? t("saving")
    : step === STEP_COUNT
      ? isEdit
        ? wasRejected
          ? tEdit("saveAndResubmit")
          : tEdit("saveChanges")
        : t("publish")
      : t("continueLabel");

  return (
    <div>
      <StepIndicator steps={steps} onSelect={goToStep} />

      <div className="flex flex-col items-start gap-10 lg:flex-row">
        <form
          ref={formRef}
          action={formAction}
          className="min-w-0 flex-1 rounded-2xl border border-ink-100 bg-white p-6 shadow-[var(--shadow-card)] sm:p-8"
        >
          {!isEdit && <input type="hidden" name="id" value={listingId ?? ""} />}

          {isEdit && wasRejected && (
            <div className="mb-6 rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
              <p className="font-medium">{tEdit("rejectedNoticeTitle")}</p>
              {listing.rejectionReason && <p className="mt-0.5">{listing.rejectionReason}</p>}
              <p className="mt-1.5">{tEdit("rejectedNoticeBody")}</p>
            </div>
          )}

          <div
            ref={(el) => {
              stepRefs.current[0] = el;
            }}
            className={step === 1 ? "" : "hidden"}
          >
            <StepTypeLocation
              propertyType={propertyType}
              onPropertyTypeChange={setPropertyType}
              transactionType={transactionType}
              onTransactionTypeChange={setTransactionType}
              raionId={raionId}
              onRaionIdChange={handleRaionIdChange}
              localitateId={localitateId}
              onLocalitateIdChange={setLocalitateId}
              chisinauSectorId={chisinauSectorId}
              onChisinauSectorIdChange={setChisinauSectorId}
              defaultCountry={location?.country}
              defaultStreetAddress={location?.street}
              defaultBuildingNumber={location?.buildingNumber}
            />
          </div>

          <div
            ref={(el) => {
              stepRefs.current[1] = el;
            }}
            className={step === 2 ? "" : "hidden"}
          >
            <StepDetails propertyType={propertyType} transactionType={transactionType} listing={listing} />
          </div>

          <div
            ref={(el) => {
              stepRefs.current[2] = el;
            }}
            className={step === 3 ? "" : "hidden"}
          >
            <StepPhotos listingId={listingId} initialPhotos={listing?.photos} deferDeletes={isEdit} />
          </div>

          <div
            ref={(el) => {
              stepRefs.current[3] = el;
            }}
            className={step === 4 ? "" : "hidden"}
          >
            <StepPriceContact transactionType={transactionType} listing={listing} publishers={isEdit ? [] : publishers} />
          </div>

          {state.error && (
            <p className="mt-6 rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
              {state.error}
            </p>
          )}

          {state.success && isEdit && (
            <p className="mt-6 rounded-xl border border-brand-100 bg-brand-100/60 px-4 py-3 text-sm text-brand-700">
              {wasRejected ? tEdit("savedAndResubmitted") : tEdit("saved")}
            </p>
          )}

          <div className="mt-8 flex items-center justify-between">
            {step > 1 ? (
              <Button type="button" variant="secondary" onClick={() => goToStep(step - 1)}>
                {t("backLabel")}
              </Button>
            ) : (
              <span />
            )}
            <Button type="button" disabled={pending} onClick={handlePrimaryClick}>
              {primaryLabel}
            </Button>
          </div>
        </form>

        <div className="w-full lg:sticky lg:top-24 lg:w-80 lg:shrink-0">
          <ListingTips />
        </div>
      </div>
    </div>
  );
}
