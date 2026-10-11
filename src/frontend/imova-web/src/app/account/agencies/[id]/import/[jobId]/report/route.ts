import { proxyFileDownload } from "@/lib/api/fileDownloadProxy";

// The import's problems as a CSV (codes + English messages), downloaded through the web app.
export async function GET(_request: Request, { params }: { params: Promise<{ id: string; jobId: string }> }) {
  const { id, jobId } = await params;
  return proxyFileDownload(`/api/v1/agencies/${encodeURIComponent(id)}/import/${encodeURIComponent(jobId)}/report.csv`);
}
