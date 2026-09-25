import { describe, expect, it } from "vitest";
import { attachmentExtension, attachmentSrc, MAX_ATTACHMENT_BYTES, pickAttachments } from "@/lib/messaging/attachments";

const file = (type: string, size = 1000) => ({ type, size });

describe("attachmentSrc", () => {
  it("points at the site's access-checked proxy, not at storage", () => {
    expect(attachmentSrc("3f2a")).toBe("/message-attachments/3f2a");
  });
});

describe("attachmentExtension", () => {
  it("maps the supported image types and rejects the rest", () => {
    expect(attachmentExtension(file("image/jpeg"))).toBe(".jpg");
    expect(attachmentExtension(file("image/png"))).toBe(".png");
    expect(attachmentExtension(file("image/webp"))).toBe(".webp");
    expect(attachmentExtension(file("image/gif"))).toBeNull();
    expect(attachmentExtension(file("application/pdf"))).toBeNull();
  });
});

describe("pickAttachments", () => {
  it("takes images up to the free slots and reports why others were skipped", () => {
    const { accepted, problems } = pickAttachments(
      [file("image/png"), file("application/pdf"), file("image/jpeg", MAX_ATTACHMENT_BYTES + 1), file("image/webp"), file("image/png")],
      3,
      5,
    );

    expect(accepted).toHaveLength(2);
    expect(problems.sort()).toEqual(["count", "size", "type"]);
  });

  it("accepts nothing when all five slots are used", () => {
    expect(pickAttachments([file("image/png")], 5, 5)).toEqual({ accepted: [], problems: ["count"] });
  });
});
