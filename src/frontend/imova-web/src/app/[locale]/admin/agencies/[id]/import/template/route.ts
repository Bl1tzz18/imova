import type { NextRequest } from "next/server";
import { proxyFileDownload } from "@/lib/api/fileDownloadProxy";
import { IMPORT_PROPERTY_TYPES } from "@/lib/agency/importReadme";

// The CSV template of one property type (?propertyType=Apartment), downloaded through the web app.
export async function GET(request: NextRequest, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const type = request.nextUrl.searchParams.get("propertyType") ?? "";
  if (!(IMPORT_PROPERTY_TYPES as readonly string[]).includes(type)) {
    return new Response("Unknown property type.", { status: 400 });
  }

  return proxyFileDownload(`/api/v1/agencies/${encodeURIComponent(id)}/import/template?propertyType=${type}`);
}
