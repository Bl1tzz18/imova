import { describe, expect, it } from "vitest";
import { photoSrcSet } from "./view";

describe("photoSrcSet", () => {
  const photo = { thumbnailUrl: "https://b/p_400.jpg", cardUrl: "https://b/p_800.jpg", url: "https://b/p_1600.jpg" };

  it("offers every size with its width", () => {
    expect(photoSrcSet(photo, "large")).toBe("https://b/p_400.jpg 400w, https://b/p_800.jpg 800w, https://b/p_1600.jpg 1600w");
  });

  it("stops at the card size for cards", () => {
    expect(photoSrcSet(photo, "card")).toBe("https://b/p_400.jpg 400w, https://b/p_800.jpg 800w");
  });

  it("is left out while the photo only has its original", () => {
    expect(photoSrcSet({ thumbnailUrl: "https://b/p.png", cardUrl: "https://b/p.png", url: "https://b/p.png" }, "large")).toBeUndefined();
  });
});
