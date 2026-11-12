import { describe, expect, it } from "vitest";
import demos from "../src/index";

describe("addresses", () => {
  it("give the box office's first page an address, and lead back to its link", () => {
    expect(demos.linkToAddress({ $feature: "demos/Performances" })).toBe("box-office/performances");
    expect(demos.addressToLink("box-office/performances")).toEqual({ $feature: "demos/Performances" });
    expect(demos.addressToLink("box-office")).toEqual({ $feature: "demos/Performances" });
  });

  it("leave every other query to the shell's default address", () => {
    expect(demos.linkToAddress({ $feature: "demos/YourTickets" })).toBeNull();
    expect(demos.addressToLink("lost-property/unclaimed-items")).toBeNull();
  });
});
