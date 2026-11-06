import { describe, expect, it } from "vitest";
import demos from "../src/index";

describe("addresses", () => {
  it("give each of the lost property office's queries an address under the demo's name", () => {
    expect(demos.linkToAddress({ $feature: "demos/UnclaimedItems" })).toBe("lost-property/unclaimed-items");
    expect(demos.linkToAddress({ $feature: "demos/ItemsFoundAt", Station: "Bristol Temple Meads" }))
      .toBe("lost-property/found-at/Bristol%20Temple%20Meads");
  });

  it("lead back to the links they were made from", () => {
    for (const link of [
      { $feature: "demos/UnclaimedItems" },
      { $feature: "demos/DonatedToCharity" },
      { $feature: "demos/YourLostReports" },
      { $feature: "demos/ItemsFoundAt", Station: "Bristol Temple Meads" },
      { $feature: "demos/Performances" },
      { $feature: "demos/YourTickets" }
    ])
      expect(demos.addressToLink(demos.linkToAddress(link)!)).toEqual(link);
  });

  it("aren't given to commands, so a reload never runs one again", () => {
    expect(demos.linkToAddress({ $feature: "demos/HandInItem", Category: null, Description: null, Station: null })).toBeNull();
    expect(demos.linkToAddress({ $feature: "demos/ClaimItem", ItemId: "F-000001", Reference: "R-000001" })).toBeNull();
    expect(demos.linkToAddress({ $feature: "demos/BuySeats", PerformanceId: "friday", Seats: null })).toBeNull();
  });

  it("start the demo at its unclaimed items, and mean nothing otherwise", () => {
    expect(demos.addressToLink("")).toEqual({ $feature: "demos/UnclaimedItems" });
    expect(demos.addressToLink("lost-property")).toEqual({ $feature: "demos/UnclaimedItems" });
    expect(demos.addressToLink("lost-property/nowhere")).toBeNull();
    expect(demos.addressToLink("box-office")).toEqual({ $feature: "demos/Performances" });
    expect(demos.addressToLink("box-office/performances")).toEqual({ $feature: "demos/Performances" });
  });
});
