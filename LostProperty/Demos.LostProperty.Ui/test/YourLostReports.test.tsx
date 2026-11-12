import { describe, expect, it } from "vitest";
import { item, link, report, setUp } from "./support";

describe("YourLostReports", () => {
  const body = (entries: unknown[]) => ({ Reports: entries, LinkToReport: link("ReportLostItem", { Category: null, Description: null }), LinkToUnclaimedItems: link("UnclaimedItems") });

  it("offers to claim a match the traveller can claim", () => {
    const { element, followed, render, click } = setUp();
    const claim = link("ClaimItem", { ItemId: "F-000007", Reference: "R-000002" });
    render("YourLostReports", body([{ Report: report("R-000002", "A black umbrella"), PossibleMatch: item("F-000007", "A black umbrella", "York"), LinkToClaim: claim }]));

    expect(element.textContent).toContain("We may have it: A black umbrella, handed in at York");
    click("Claim it");
    expect(followed).toEqual([claim]);
  });

  it("says what became of a match there's no claiming, and when there's none", () => {
    const { element, render } = setUp();
    render("YourLostReports", body([
      { Report: report("R-000002", "A black umbrella"), PossibleMatch: item("F-000007", "A black umbrella", "York", "Claimed", "traveller"), LinkToClaim: null },
      { Report: report("R-000003", "A left glove"), PossibleMatch: null, LinkToClaim: null }
    ]));

    expect(element.textContent).toContain("You claimed it: A black umbrella.");
    expect(element.textContent).toContain("Nothing like it has been handed in yet.");
    expect(element.textContent).not.toContain("Claim it");
  });

  it("offers to report something lost", () => {
    const { followed, render, click } = setUp();
    render("YourLostReports", body([]));

    click("Report something lost");
    expect(followed).toEqual([link("ReportLostItem", { Category: null, Description: null })]);
  });
});
