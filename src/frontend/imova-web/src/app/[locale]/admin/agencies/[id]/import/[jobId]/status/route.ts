import { forwardToApi } from "@/lib/api/imageUploadProxy";

// One import's state, polled by its result page while it runs.
export async function GET(_request: Request, { params }: { params: Promise<{ id: string; jobId: string }> }) {
  const { id, jobId } = await params;
  return forwardToApi(`/api/v1/agencies/${encodeURIComponent(id)}/import/${encodeURIComponent(jobId)}`, { method: "GET", cache: "no-store" });
}
