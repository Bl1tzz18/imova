"use client";

import { useActionState } from "react";
import { createProperty, type CreatePropertyState } from "./actions";

const initialState: CreatePropertyState = {};

export function PropertyForm() {
  const [state, formAction, pending] = useActionState(createProperty, initialState);

  return (
    <form
      action={formAction}
      style={{ display: "flex", flexDirection: "column", gap: "0.75rem", maxWidth: "28rem" }}
    >
      <label>
        Titlu
        <input name="title" required maxLength={200} />
      </label>
      <label>
        Preț
        <input name="price" type="number" min="0.01" step="0.01" required />
      </label>
      <label>
        Monedă
        <input name="currency" required minLength={3} maxLength={3} pattern="[A-Za-z]{3}" defaultValue="EUR" />
      </label>
      <label>
        Oraș
        <input name="city" required maxLength={100} />
      </label>
      <label>
        Sector/Localitate
        <input name="district" required maxLength={100} />
      </label>

      {state.error && <p style={{ color: "crimson" }}>{state.error}</p>}

      <button type="submit" disabled={pending}>
        {pending ? "Se salvează..." : "Publică anunțul"}
      </button>
    </form>
  );
}
