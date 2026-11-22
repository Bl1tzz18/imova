import { forwardToApi, proxyImageUpload } from "@/lib/api/imageUploadProxy";
import { PROFILE_PICTURE_MAX_BYTES } from "@/lib/api/uploadSize";

// Same-origin proxy for the signed-in account's profile picture: upload (POST, multipart "file", up
// to 5 MB like the API) and remove (DELETE). Answers the profile ({ profilePictureUrl, … }) or { error }.
export function POST(request: Request) {
  return proxyImageUpload(request, "/api/v1/users/me/profile-picture", PROFILE_PICTURE_MAX_BYTES);
}

export function DELETE() {
  return forwardToApi("/api/v1/users/me/profile-picture", { method: "DELETE" });
}
