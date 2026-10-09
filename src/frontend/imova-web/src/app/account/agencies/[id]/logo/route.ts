import { getTranslations } from "next-intl/server";
import { apiErrorMessage } from "@/lib/api/errorMessage";
import { getSessionToken } from "@/lib/auth/session";

// Same-origin proxy for an agency's logo: upload (POST, multipart "file") and remove (DELETE). A
// route handler rather than a server action because a logo may be up to 5 MB and server actions
// stop at 1 MB. Answers { logoUrl, logoThumbnailUrl } or { error } (translated), with the API's status.

const MAX_BYTES = 5 * 1024 * 1024;
// Multipart framing on top of the file itself.
const MAX_BODY_BYTES = MAX_BYTES + 64 * 1024;

type LogoResult = { logoUrl: string | null; logoThumbnailUrl: string | null } | { error: string };

async function forward(id: string, init: RequestInit): Promise<Response> {
  const token = await getSessionToken();
  if (!token) {
    return Response.json({ error: (await getTranslations("Errors"))("status.401") } satisfies LogoResult, { status: 401 });
  }

  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}/api/v1/agencies/${encodeURIComponent(id)}/logo`, {
    ...init,
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) {
    return Response.json({ error: await apiErrorMessage(res) } satisfies LogoResult, { status: res.status });
  }

  const agency = (await res.json()) as { logoUrl: string | null; logoThumbnailUrl: string | null };
  return Response.json({ logoUrl: agency.logoUrl, logoThumbnailUrl: agency.logoThumbnailUrl } satisfies LogoResult);
}

async function tooLarge(): Promise<Response> {
  const t = await getTranslations("Errors");
  return Response.json({ error: t("codes.upload.tooLarge", { maxMb: MAX_BYTES / (1024 * 1024) }) } satisfies LogoResult, {
    status: 413,
  });
}

export async function POST(request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  if (Number(request.headers.get("content-length") ?? 0) > MAX_BODY_BYTES) {
    return tooLarge();
  }

  const file = (await request.formData()).get("file");
  if (!(file instanceof File)) {
    return Response.json({ error: (await getTranslations("Errors"))("status.400") } satisfies LogoResult, { status: 400 });
  }
  if (file.size > MAX_BYTES) {
    return tooLarge();
  }

  const body = new FormData();
  body.append("file", file, file.name);
  return forward(id, { method: "POST", body });
}

export async function DELETE(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return forward(id, { method: "DELETE" });
}
