"use client";

import { useActionState } from "react";
import { useTranslations } from "next-intl";
import { updateProfile, type AuthFormState } from "@/lib/auth/actions";
import type { UserProfile } from "@/lib/auth/profile";
import { Button } from "@/components/ui/Button";
import { FieldLabel, TextInput } from "@/components/ui/Field";
import { PhoneInput } from "@/components/ui/PhoneInput";
import { ProfilePictureUploader } from "@/components/account/ProfilePictureUploader";

const initialState: AuthFormState = {};

export function ProfileForm({ profile }: { profile: UserProfile }) {
  const tAuth = useTranslations("Auth");
  const tAccount = useTranslations("Account");
  const [state, formAction, pending] = useActionState(updateProfile, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-3.5">
      <ProfilePictureUploader
        userId={profile.id}
        displayName={profile.displayName}
        email={profile.email}
        profilePictureUrl={profile.profilePictureUrl}
      />

      <label className="block">
        <FieldLabel>{tAuth("nameLabel")}</FieldLabel>
        <TextInput type="text" name="name" defaultValue={profile.displayName ?? ""} placeholder={tAuth("namePlaceholder")} />
      </label>

      <label className="block">
        <FieldLabel>{tAuth("phoneLabel")}</FieldLabel>
        <PhoneInput name="phone" defaultValue={profile.phoneNumber ?? undefined} required />
      </label>

      <label className="block">
        <FieldLabel>{tAuth("emailLabel")}</FieldLabel>
        <TextInput type="email" value={profile.email} disabled className="cursor-not-allowed opacity-60" />
      </label>

      {state.error && (
        <p className="rounded-xl border border-accent-100 bg-accent-100/60 px-4 py-3 text-sm text-accent-700">
          {state.error}
        </p>
      )}
      {state.success && (
        <p className="rounded-xl border border-brand-100 bg-brand-100/60 px-4 py-3 text-sm text-brand-700">
          {tAccount("profileSaved")}
        </p>
      )}

      <Button type="submit" disabled={pending} className="mt-1.5 self-start">
        {pending ? tAccount("saving") : tAccount("saveChanges")}
      </Button>
    </form>
  );
}
