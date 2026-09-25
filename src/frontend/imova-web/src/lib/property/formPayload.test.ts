import { describe, expect, it } from "vitest";
import { buildListingPayload } from "@/lib/property/formPayload";

describe("listing payload", () => {
  function form(checked: Record<string, string[]>) {
    const data = new FormData();
    data.set("propertyType", "Apartment");
    data.set("transactionType", "Sale");
    for (const [name, values] of Object.entries(checked)) for (const value of values) data.append(name, value);
    return data;
  }

  it("sends the checked amenities and proximities as separate lists", () => {
    const payload = buildListingPayload(form({ amenityIds: ["balcony"], proximityIds: ["school", "park"] }));
    expect(payload.amenityIds).toEqual(["balcony"]);
    expect(payload.proximityIds).toEqual(["school", "park"]);
  });

  it("sends an empty proximity list when none is checked (it's optional)", () => {
    expect(buildListingPayload(form({})).proximityIds).toEqual([]);
  });
});
