import { proxyImageUpload } from "@/lib/api/imageUploadProxy";
import { IMPORT_MAX_BYTES } from "@/lib/agency/importJobs";

// Uploads a CSV (multipart "file", up to 5 MB like the API) for the agency's import — a route handler
// because a server action stops at 1 MB. Answers the new import ({ id, status, … }) or { error }.
export async function POST(request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return proxyImageUpload(request, `/api/v1/agencies/${encodeURIComponent(id)}/import`, IMPORT_MAX_BYTES);
}
