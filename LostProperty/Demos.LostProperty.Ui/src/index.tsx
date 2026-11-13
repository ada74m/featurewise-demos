import { featurewiseModule } from "demos-common-ui";
import * as claimItem from "./features/ClaimItem";
import * as handInItem from "./features/HandInItem";
import * as reportLostItem from "./features/ReportLostItem";
import * as unclaimedItems from "./features/UnclaimedItems";
import * as yourLostReports from "./features/YourLostReports";

// The lost property office's UI module, a feature at a time: each query's address, and each feature's
// renderers, by result case. Each feature's renderers are in its file in features/. Anything not here,
// such as DonatedToCharity, or ItemsFoundAt's result, is left to the shell: its generic renderer, and
// its default address for a query. The service's own path leads to the unclaimed items.

export const ui = featurewiseModule("lost-property", { home: "unclaimed-items" });

ui.addQuery("UnclaimedItems")
  .address("unclaimed-items")
  .render(unclaimedItems.result);

ui.addQuery("ItemsFoundAt")
  .address("found-at/{Station}");

ui.addQuery("YourLostReports")
  .address("your-reports")
  .render(yourLostReports.result);

ui.addCommand("HandInItem")
  .renderCase("NeedsDetails", handInItem.NeedsDetails)
  .renderCase("HandedIn", handInItem.HandedIn);

ui.addCommand("ReportLostItem")
  .renderCase("NeedsDetails", reportLostItem.NeedsDetails)
  .renderCase("Reported", reportLostItem.Reported)
  .renderCase("MayAlreadyHaveIt", reportLostItem.MayAlreadyHaveIt);

ui.addCommand("ClaimItem")
  .renderCase("Claimed", claimItem.Claimed)
  .renderCase("DoesntMatchReport", claimItem.DoesntMatchReport)
  .renderCase("AlreadyClaimed", claimItem.AlreadyClaimed)
  .renderCase("NotFound", claimItem.NotFound);

export default ui;
