import { describe, expect, it } from "vitest";
import { MULTIPART_OVERHEAD_BYTES, PROFILE_PICTURE_MAX_BYTES, uploadSizeProblem } from "./uploadSize";

const MB = 1024 * 1024;

describe("uploadSizeProblem", () => {
  it("lets a 4.35 MB phone photo through — more than a server action's 1 MB", () => {
    expect(uploadSizeProblem({ fileSize: 4_352_835, maxBytes: PROFILE_PICTURE_MAX_BYTES })).toBeNull();
    expect(uploadSizeProblem({ contentLength: String(4_352_835 + 500), maxBytes: PROFILE_PICTURE_MAX_BYTES })).toBeNull();
  });

  it("takes exactly the limit and refuses a byte more", () => {
    expect(uploadSizeProblem({ fileSize: 5 * MB, maxBytes: 5 * MB })).toBeNull();
    expect(uploadSizeProblem({ fileSize: 5 * MB + 1, maxBytes: 5 * MB })).toBe("tooLarge");
  });

  it("allows the multipart framing on top of the file in the Content-Length", () => {
    expect(uploadSizeProblem({ contentLength: String(5 * MB + MULTIPART_OVERHEAD_BYTES), maxBytes: 5 * MB })).toBeNull();
    expect(uploadSizeProblem({ contentLength: String(5 * MB + MULTIPART_OVERHEAD_BYTES + 1), maxBytes: 5 * MB })).toBe("tooLarge");
  });

  it("has no opinion without a size", () => {
    expect(uploadSizeProblem({ contentLength: null, maxBytes: 5 * MB })).toBeNull();
    expect(uploadSizeProblem({ contentLength: "garbage", maxBytes: 5 * MB })).toBeNull();
  });
});
