import claimItem from "./features/ClaimItem";
import handInItem from "./features/HandInItem";
import itemsFoundAt from "./features/ItemsFoundAt";
import reportLostItem from "./features/ReportLostItem";
import unclaimedItems from "./features/UnclaimedItems";
import yourLostReports from "./features/YourLostReports";
import { lostProperty } from "./module";

// The lost property office's module, dressing the office one feature at a time. A feature not added
// here, such as DonatedToCharity, or a result case its file doesn't render, is left to the shell: its
// generic renderer, and its default address for a query.

unclaimedItems(lostProperty);
itemsFoundAt(lostProperty);
yourLostReports(lostProperty);
handInItem(lostProperty);
reportLostItem(lostProperty);
claimItem(lostProperty);

export default lostProperty;
