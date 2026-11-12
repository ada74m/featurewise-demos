import { describe, expect, it } from "vitest";
import lostProperty from "../src/index";
import { link } from "./support";

describe("addresses", () => {
  it("give the pages worth sharing a readable address, and lead back to their links", () => {
    for (const [address, query] of [
      ["unclaimed-items", link("UnclaimedItems")],
      ["your-reports", link("YourLostReports")],
      ["found-at/Bristol%20Temple%20Meads", link("ItemsFoundAt", { Station: "Bristol Temple Meads" })]
    ] as const) {
      expect(lostProperty.linkToAddress(query)).toBe(address);
      expect(lostProperty.addressToLink(address)).toEqual(query);
    }
    expect(lostProperty.addressToLink("")).toEqual(link("UnclaimedItems"));
  });

  it("leave every other query to the shell's default address, and give a command none", () => {
    expect(lostProperty.linkToAddress(link("DonatedToCharity"))).toBeNull();
    expect(lostProperty.linkToAddress(link("ClaimItem", { ItemId: "F-000001", Reference: "R-000001" }))).toBeNull();
    expect(lostProperty.addressToLink("nowhere")).toBeNull();
  });
});
