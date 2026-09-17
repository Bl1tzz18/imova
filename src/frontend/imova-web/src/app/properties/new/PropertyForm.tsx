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
import { createProperty, type CreatePropertyState } from "./actions";

const initialState: CreatePropertyState = {};

// No auth yet, so there's no logged-in user to attribute the listing to.
// Prefilled with the seeded demo user until real auth exists.
const DEMO_OWNER_ID = "33333333-3333-3333-3333-333333333333";

const STEP_COUNT = 4;

export function PropertyForm() {
  const [state, formAction, pending] = useActionState(createProperty, initialState);
  const [step, setStep] = useState(1);
  const [propertyType, setPropertyType] = useState("Apartment");
  const [listingType, setListingType] = useState("Rent");

  // Generated up front (client-side only, in an effect — crypto.randomUUID() during the
  // initial render would produce a different value on the server than on the client and
  // trigger a hydration mismatch) so photos can be uploaded and attached server-side (see
  // ImageUploader/actions.ts) before the property itself is created.
  const [propertyId, setPropertyId] = useState<string | null>(null);
  useEffect(() => setPropertyId(crypto.randomUUID()), []);

  const formRef = useRef<HTMLFormElement>(null);
  const stepRefs = useRef<Array<HTMLDivElement | null>>([]);

  const t = useTranslations("PropertyForm");

  // Only the currently visible step's fields are validated: a hidden field can't show its
  // native validation bubble, so jumping straight to a distant step (skipping ones never
  // rendered visible) is not allowed — advancing one step at a time keeps every check on a
  // field the browser can actually focus and report against.
  function validateCurrentStep(): boolean {
    const container = stepRefs.current[step - 1];
    if (!container) return true;
    const invalid = container.querySelector<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>(":invalid");
    if (invalid) {
      invalid.reportValidity();
      return false;
    }
    return true;
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

  if (state.success) {
    return <SuccessPanel />;
  }

  return (
    <div>
      <StepIndicator steps={steps} onSelect={goToStep} />

      <div className="flex flex-col items-start gap-10 lg:flex-row">
        <form
          ref={formRef}
          action={formAction}
          className="min-w-0 flex-1 rounded-2xl border border-ink-100 bg-white p-6 shadow-[var(--shadow-card)] sm:p-8"
        >
          <input type="hidden" name="id" value={propertyId ?? ""} />

          <div
            ref={(el) => {
              stepRefs.current[0] = el;
            }}
            className={step === 1 ? "" : "hidden"}
          >
            <StepTypeLocation
              propertyType={propertyType}
              onPropertyTypeChange={setPropertyType}
              listingType={listingType}
              onListingTypeChange={setListingType}
            />
          </div>

          <div
            ref={(el) => {
              stepRefs.current[1] = el;
            }}
            className={step === 2 ? "" : "hidden"}
          >
            <StepDetails propertyType={propertyType} listingType={listingType} />
          </div>

          <div
            ref={(el) => {
              stepRefs.current[2] = el;
            }}
            className={step === 3 ? "" : "hidden"}
          >
            <StepPhotos propertyId={propertyId} />
          </div>

          <div
            ref={(el) => {
              stepRefs.current[3] = el;
            }}
            className={step === 4 ? "" : "hidden"}
          >
            <StepPriceContact ownerId={DEMO_OWNER_ID} />
          </div>

          {state.error && (
            <p className="mt-6 rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
              {state.error}
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
              {pending ? t("saving") : step === STEP_COUNT ? t("publish") : t("continueLabel")}
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
