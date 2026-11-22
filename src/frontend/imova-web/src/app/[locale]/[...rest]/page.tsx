import { notFound } from "next/navigation";

// Catches every address under a language that no page matches, so the 404 renders inside the
// language's layout (header, footer, its words) — see ../not-found.tsx.
export default function CatchAll() {
  notFound();
}
