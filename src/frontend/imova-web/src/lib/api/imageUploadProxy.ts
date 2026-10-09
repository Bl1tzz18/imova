import { getTranslations } from "next-intl/server";
import { apiErrorMessage } from "@/lib/api/errorMessage";
import { getSessionToken } from "@/lib/auth/session";
import { uploadSizeProblem } from "@/lib/api/uploadSize";

// Server-side only. The same-origin route handlers that pass one uploaded image (multipart "file")
// on to an authenticated API endpoint with the session token — the profile picture and an agency's
// logo. Route handlers rather than server actions: a server action's body stops at 1 MB, these
// images may be 5 MB. Answers the API's JSON on success, else { error } (translated) with its status.

export async function proxyImageUpload(request: Request, apiPath: string, maxBytes: number): Promise<Response> {
  const t = await getTranslations("Errors");
  const tooLarge = () =>
    Response.json({ error: t("codes.upload.tooLarge", { maxMb: maxBytes / (1024 * 1024) }) }, { status: 413 });

  if (uploadSizeProblem({ contentLength: request.headers.get("content-length"), maxBytes }) === "tooLarge") {
    return tooLarge();
  }

  const file = (await request.formData()).get("file");
  if (!(file instanceof File)) {
    return Response.json({ error: t("status.400") }, { status: 400 });
  }
  if (uploadSizeProblem({ fileSize: file.size, maxBytes }) === "tooLarge") {
    return tooLarge();
  }

  const body = new FormData();
  body.append("file", file, file.name);
  return forwardToApi(apiPath, { method: "POST", body });
}

// DELETE (or any body-less call) to the API with the session token, answered the same way.
export async function forwardToApi(apiPath: string, init: RequestInit): Promise<Response> {
  const token = await getSessionToken();
  if (!token) {
    return Response.json({ error: (await getTranslations("Errors"))("status.401") }, { status: 401 });
  }

  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const res = await fetch(`${apiUrl}${apiPath}`, { ...init, headers: { Authorization: `Bearer ${token}` } });
  if (!res.ok) {
    return Response.json({ error: await apiErrorMessage(res) }, { status: res.status });
  }
  return Response.json(await res.json());
}
