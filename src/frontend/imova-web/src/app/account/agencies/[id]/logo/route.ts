import { forwardToApi, proxyImageUpload } from "@/lib/api/imageUploadProxy";
import { AGENCY_LOGO_MAX_BYTES } from "@/lib/api/uploadSize";

// Same-origin proxy for an agency's logo: upload (POST, multipart "file", up to 5 MB like the API)
// and remove (DELETE). Answers the agency ({ logoUrl, logoThumbnailUrl, … }) or { error }.
export async function POST(request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return proxyImageUpload(request, `/api/v1/agencies/${encodeURIComponent(id)}/logo`, AGENCY_LOGO_MAX_BYTES);
}

export async function DELETE(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return forwardToApi(`/api/v1/agencies/${encodeURIComponent(id)}/logo`, { method: "DELETE" });
}
