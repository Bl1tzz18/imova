import { describe, expect, it } from "vitest";
import { listingExpiry } from "./expiry";

const now = new Date("2027-03-01T12:00:00Z");

describe("listingExpiry", () => {
  it("has nothing to show for a listing that isn't Active", () => {
    expect(listingExpiry({ status: "Expired", expiresAt: "2027-02-01T00:00:00Z" }, now)).toEqual({ kind: "none" });
    expect(listingExpiry({ status: "Draft", expiresAt: null }, now)).toEqual({ kind: "none" });
  });

  it("has nothing to show for an Active listing without an expiry date", () => {
    expect(listingExpiry({ status: "Active", expiresAt: null }, now)).toEqual({ kind: "none" });
  });

  it("isn't renewable while the end is more than a week away", () => {
    const expiry = listingExpiry({ status: "Active", expiresAt: "2027-06-01T12:00:00Z" }, now);
    expect(expiry).toMatchObject({ kind: "active", daysLeft: 92, renewable: false });
  });

  it("is renewable in the last week, counting partial days up", () => {
    expect(listingExpiry({ status: "Active", expiresAt: "2027-03-08T12:00:00Z" }, now)).toMatchObject({
      daysLeft: 7,
      renewable: true,
    });
    expect(listingExpiry({ status: "Active", expiresAt: "2027-03-01T13:00:00Z" }, now)).toMatchObject({
      daysLeft: 1,
      renewable: true,
    });
  });

  it("never counts below zero days once the date has passed but the job hasn't run yet", () => {
    expect(listingExpiry({ status: "Active", expiresAt: "2027-02-28T12:00:00Z" }, now)).toMatchObject({
      daysLeft: 0,
      renewable: true,
    });
  });
});
