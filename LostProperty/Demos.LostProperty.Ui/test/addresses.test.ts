import { describe, expect, it } from "vitest";
import lostProperty from "../src/index";

describe("addresses", () => {
  it("give the office's first page an address, and lead back to its link", () => {
    expect(lostProperty.linkToAddress({ $feature: "lost-property/UnclaimedItems" })).toBe("unclaimed-items");
    expect(lostProperty.addressToLink("unclaimed-items")).toEqual({ $feature: "lost-property/UnclaimedItems" });
    expect(lostProperty.addressToLink("")).toEqual({ $feature: "lost-property/UnclaimedItems" });
  });

  it("leave every other query to the shell's default address", () => {
    expect(lostProperty.linkToAddress({ $feature: "lost-property/YourLostReports" })).toBeNull();
    expect(lostProperty.addressToLink("nowhere")).toBeNull();
  });
});
