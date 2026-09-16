// Runs in the browser (called from the client-side ImageUploader), so this needs the
// browser-reachable API origin — NEXT_PUBLIC_API_URL, not the server-only API_URL used by
// Server Components/Actions. Falls back to localhost:8080 to match the pattern used everywhere
// else in this app for local (non-Docker) dev.
export function getBrowserApiUrl(): string {
  return process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";
}

export type UploadUrlResponse = {
  uploadUrl: string;
  blobName: string;
  expiresAt: string;
};

export type PropertyMediaResponse = {
  id: string;
  propertyId: string;
  url: string;
  contentType: string;
  fileSizeBytes: number;
  moderationStatus: string;
  sortOrder: number;
  createdAt: string;
};

async function readErrorMessage(res: Response): Promise<string> {
  const problem = await res.json().catch(() => null);
  if (problem?.errors) {
    return Object.values(problem.errors as Record<string, string[]>).flat().join(" ");
  }
  return `Request failed (${res.status})`;
}

export async function requestUploadUrl(propertyId: string, fileExtension: string): Promise<UploadUrlResponse> {
  const res = await fetch(`${getBrowserApiUrl()}/api/v1/properties/${propertyId}/media/upload-url`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ fileExtension }),
  });

  if (!res.ok) {
    throw new Error(await readErrorMessage(res));
  }

  return res.json();
}

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

export async function confirmMediaUpload(propertyId: string, blobName: string): Promise<PropertyMediaResponse> {
  const res = await fetch(`${getBrowserApiUrl()}/api/v1/properties/${propertyId}/media/confirm`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ blobName }),
  });

  if (!res.ok) {
    throw new Error(await readErrorMessage(res));
  }

  return res.json();
}
