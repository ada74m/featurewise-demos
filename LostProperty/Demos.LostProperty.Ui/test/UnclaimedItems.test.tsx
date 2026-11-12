import { describe, expect, it } from "vitest";
import { item, link, setUp } from "./support";

describe("UnclaimedItems", () => {
  const crewe = link("ItemsFoundAt", { Station: "Crewe" });
  const body = (handIn: unknown) => ({
    Items: [{ Item: item("F-000001", "A black umbrella"), LinkToStation: crewe }],
    LinkToHandIn: handIn,
    LinkToYourReports: link("YourLostReports"),
    LinkToDonated: link("DonatedToCharity")
  });

  it("lists the items, each station an anchor to what was found there", () => {
    const { element, followed, render, click } = setUp();
    render("UnclaimedItems", body(null));

    expect(element.querySelector("td")!.textContent).toContain("A black umbrella");
    const station = [...element.querySelectorAll("a")].find(a => a.textContent === "Crewe")!;
    expect(station.getAttribute("href")).toBe("/lost-property/found-at/Crewe");
    click("Crewe");
    expect(followed).toEqual([crewe]);
  });

  it("offers the button to hand something in only when the link is there", () => {
    const traveller = setUp();
    traveller.render("UnclaimedItems", body(null));
    expect(traveller.element.textContent).not.toContain("Hand in an item");

    const clerk = setUp();
    const handIn = link("HandInItem", { Category: null, Description: null, Station: null });
    clerk.render("UnclaimedItems", body(handIn));
    clerk.click("Hand in an item");
    expect(clerk.followed).toEqual([handIn]);
  });
});
