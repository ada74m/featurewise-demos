import { describe, expect, it } from "vitest";
import { item, link, setUp } from "./support";

describe("HandInItem", () => {
  it("asks for the missing details in a form, and follows the link with them filled in", () => {
    const { followed, render, type, submit } = setUp();
    const handIn = link("HandInItem", { Category: "Umbrella", Description: null, Station: null });
    render("HandInItem", { $case: "NeedsDetails", Missing: ["Description", "Station"], LinkToHandIn: handIn });

    type("Description", "Black, with a duck's head handle");
    type("Station", "York");
    submit();

    expect(followed).toEqual([{ ...handIn, Category: "Umbrella", Description: "Black, with a duck's head handle", Station: "York" }]);
  });

  it("says what was handed in, and goes back to the unclaimed items", () => {
    const { followed, toasts, render } = setUp();
    render("HandInItem", { $case: "HandedIn", Item: item("F-000007", "A black umbrella"), LinkToUnclaimedItems: link("UnclaimedItems") });

    expect(toasts).toEqual(["Handed in: A black umbrella."]);
    expect(followed).toEqual([link("UnclaimedItems")]);
  });
});
