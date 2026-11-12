import { describe, expect, it } from "vitest";
import { item, link, report, setUp } from "./support";

describe("ReportLostItem", () => {
  it("asks for what's missing, then follows the link with it", () => {
    const { followed, render, type, submit } = setUp();
    const reportLost = link("ReportLostItem", { Category: null, Description: null });
    render("ReportLostItem", { $case: "NeedsDetails", Missing: ["Category", "Description"], LinkToReport: reportLost });

    type("Category", "Glove");
    type("Description", "A left glove, red wool");
    submit();
    expect(followed).toEqual([{ ...reportLost, Category: "Glove", Description: "A left glove, red wool" }]);
  });

  it("gives the reference, and goes to the traveller's reports", () => {
    const { followed, toasts, render } = setUp();
    render("ReportLostItem", { $case: "Reported", Report: report("R-000003", "A left glove"), LinkToYourReports: link("YourLostReports") });

    expect(toasts).toEqual(["Reported. Your reference is R-000003."]);
    expect(followed).toEqual([link("YourLostReports")]);
  });

  it("offers the claim straight away when the office may already have it", () => {
    const { element, followed, render, click } = setUp();
    const claim = link("ClaimItem", { ItemId: "F-000002", Reference: "R-000003" });
    render("ReportLostItem", { $case: "MayAlreadyHaveIt", Report: report("R-000003", "A left glove"), Item: item("F-000002", "A left glove", "Leeds"), LinkToClaim: claim, LinkToYourReports: link("YourLostReports") });

    expect(element.textContent).toContain("A left glove was handed in at Leeds");
    click("Claim it");
    expect(followed).toEqual([claim]);
  });
});
