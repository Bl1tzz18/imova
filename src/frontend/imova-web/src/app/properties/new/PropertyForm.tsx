"use client";

import { useActionState } from "react";
import { createProperty, type CreatePropertyState } from "./actions";

const initialState: CreatePropertyState = {};

// No auth yet, so there's no logged-in user to attribute the listing to.
// Prefilled with the seeded demo user until real auth exists.
const DEMO_OWNER_ID = "33333333-3333-3333-3333-333333333333";

export function PropertyForm() {
  const [state, formAction, pending] = useActionState(createProperty, initialState);

  return (
    <form
      action={formAction}
      style={{ display: "flex", flexDirection: "column", gap: "0.75rem", maxWidth: "28rem" }}
    >
      <label>
        Owner ID (temporar, cât timp nu există autentificare)
        <input name="ownerId" required defaultValue={DEMO_OWNER_ID} />
      </label>
      <label>
        Titlu
        <input name="title" required maxLength={200} />
      </label>
      <label>
        Descriere
        <textarea name="description" required maxLength={4000} rows={4} />
      </label>
      <label>
        Tip proprietate
        <select name="propertyType" required defaultValue="Apartment">
          <option value="Apartment">Apartament</option>
          <option value="House">Casă</option>
          <option value="Land">Teren</option>
          <option value="Commercial">Spațiu comercial</option>
          <option value="Garage">Garaj</option>
          <option value="Room">Cameră</option>
        </select>
      </label>
      <label>
        Tip anunț
        <select name="listingType" required defaultValue="Rent">
          <option value="Rent">Chirie</option>
          <option value="Sale">Vânzare</option>
        </select>
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
        Țară
        <input name="country" required maxLength={100} defaultValue="Moldova" />
      </label>
      <label>
        Oraș
        <input name="city" required maxLength={100} />
      </label>
      <label>
        Sector/Localitate
        <input name="district" maxLength={100} />
      </label>
      <label>
        Latitudine
        <input name="latitude" type="number" step="any" min="-90" max="90" required defaultValue="47.0105" />
      </label>
      <label>
        Longitudine
        <input name="longitude" type="number" step="any" min="-180" max="180" required defaultValue="28.8638" />
      </label>

      {state.error && <p style={{ color: "crimson" }}>{state.error}</p>}

      <button type="submit" disabled={pending}>
        {pending ? "Se salvează..." : "Publică anunțul"}
      </button>
    </form>
  );
}
