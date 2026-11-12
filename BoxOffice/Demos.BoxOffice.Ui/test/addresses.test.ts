import { describe, expect, it } from "vitest";
import boxOffice from "../src/index";

describe("addresses", () => {
  it("give the box office's first page an address, and lead back to its link", () => {
    expect(boxOffice.linkToAddress({ $feature: "box-office/Performances" })).toBe("performances");
    expect(boxOffice.addressToLink("performances")).toEqual({ $feature: "box-office/Performances" });
    expect(boxOffice.addressToLink("")).toEqual({ $feature: "box-office/Performances" });
  });

  it("leave every other query to the shell's default address", () => {
    expect(boxOffice.linkToAddress({ $feature: "box-office/YourTickets" })).toBeNull();
    expect(boxOffice.addressToLink("nowhere")).toBeNull();
  });
});
