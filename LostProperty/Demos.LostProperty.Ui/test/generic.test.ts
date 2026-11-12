import { describe, expect, it } from "vitest";
import { item, link, setUp } from "./support";

describe("the features left to the generic renderer", () => {
  it("are ItemsFoundAt and DonatedToCharity, and any case nobody has dressed", () => {
    const { render } = setUp();

    expect(render("ItemsFoundAt", { Station: "Crewe", Items: [item("F-000001", "A black umbrella")], LinkToAllStations: link("UnclaimedItems") })).toBeNull();
    expect(render("DonatedToCharity", { Items: [], LinkToUnclaimedItems: link("UnclaimedItems") })).toBeNull();
    expect(render("ClaimItem", { $case: "SomethingNew" })).toBeNull();
  });
});
