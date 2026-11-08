import { describe, expect, it } from "vitest";
import demos from "../src/index";

describe("addresses", () => {
  it("give each demo's first page an address under the demo's name, and lead back to its link", () => {
    for (const [address, link] of [
      ["lost-property/unclaimed-items", { $feature: "demos/UnclaimedItems" }],
      ["box-office/performances", { $feature: "demos/Performances" }]
    ] as const) {
      expect(demos.linkToAddress(link)).toBe(address);
      expect(demos.addressToLink(address)).toEqual(link);
    }
  });

  it("leave every other query to the shell's default address", () => {
    expect(demos.linkToAddress({ $feature: "demos/YourLostReports" })).toBeNull();
    expect(demos.linkToAddress({ $feature: "demos/ItemsFoundAt", Station: "Bristol Temple Meads" })).toBeNull();
  });

  it("start each demo at its first page, and mean nothing otherwise", () => {
    expect(demos.addressToLink("")).toEqual({ $feature: "demos/UnclaimedItems" });
    expect(demos.addressToLink("lost-property")).toEqual({ $feature: "demos/UnclaimedItems" });
    expect(demos.addressToLink("box-office")).toEqual({ $feature: "demos/Performances" });
    expect(demos.addressToLink("lost-property/nowhere")).toBeNull();
  });
});
