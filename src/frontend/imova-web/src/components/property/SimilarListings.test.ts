import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { SimilarListings } from "./SimilarListings";

describe("SimilarListings", () => {
  it("renders nothing — not even the heading — when the API found no similar listings", () => {
    expect(renderToStaticMarkup(createElement(SimilarListings, { listings: [], title: "Anunțuri asemănătoare" }))).toBe("");
  });
});
