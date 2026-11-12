import { describe, expect, it } from "vitest";
import { item, link, setUp } from "./support";

describe("ClaimItem", () => {
  it("says where to collect a claimed item, and goes to the traveller's reports", () => {
    const { followed, toasts, render } = setUp();
    render("ClaimItem", { $case: "Claimed", Item: item("F-000007", "A black umbrella", "York"), LinkToYourReports: link("YourLostReports") });

    expect(toasts).toEqual(["Claimed: A black umbrella. Collect it from York."]);
    expect(followed).toEqual([link("YourLostReports")]);
  });

  it("turns a reference that doesn't match into a way forward", () => {
    const { element, followed, render, type, submit } = setUp();
    const claim = link("ClaimItem", { ItemId: "F-000007", Reference: "R-999999" });
    render("ClaimItem", { $case: "DoesntMatchReport", ItemId: "F-000007", Reference: "R-999999", LinkToClaim: claim, LinkToYourReports: link("YourLostReports") });

    expect(element.querySelector("a")!.getAttribute("href")).toBe("/lost-property/your-reports");
    type("Reference", "R-000002");
    submit();
    expect(followed).toEqual([{ ...claim, Reference: "R-000002" }]);
  });

  it("says when an item is already claimed, or doesn't exist", () => {
    const already = setUp();
    already.render("ClaimItem", { $case: "AlreadyClaimed", ItemId: "F-000007" });
    expect(already.element.textContent).toContain("Someone has already claimed that item.");

    const missing = setUp();
    missing.render("ClaimItem", { $case: "NotFound", ItemId: "F-999999" });
    expect(missing.element.textContent).toContain("There's no item F-999999.");
  });
});
