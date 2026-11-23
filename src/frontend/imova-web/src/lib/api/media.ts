import { readPublicConfig } from "@/lib/api/publicConfig";

// Runs in the browser (uploads, typeaheads, the messaging hub), so this needs the browser-reachable
// API origin — PUBLIC_API_URL, read at server start and handed to the page (lib/api/publicConfig),
// not the server-only API_URL used by Server Components/Actions. Localhost:8080 when unset.
export function getBrowserApiUrl(): string {
  return readPublicConfig().apiUrl;
}

// Asking for an upload URL and confirming the upload need the session token, so they're server
// actions (requestPhotoUploadUrl / confirmPhotoUpload in lib/property/actions.ts); only the file
// transfer itself happens here in the browser.

// PUTs straight to Azure Blob Storage (Azurite locally) using the SAS URL — the file never
// passes through our backend. x-ms-blob-type is mandatory for a block blob PUT against the
// Azure Blob REST API.
export async function uploadFileToBlob(uploadUrl: string, file: File): Promise<void> {
  const res = await fetch(uploadUrl, {
    method: "PUT",
    headers: {
      "x-ms-blob-type": "BlockBlob",
      "Content-Type": file.type || "application/octet-stream",
    },
    body: file,
  });

  if (!res.ok) {
    throw new Error(`Upload to storage failed (${res.status})`);
  }
}
