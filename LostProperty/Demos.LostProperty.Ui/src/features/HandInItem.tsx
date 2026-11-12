import { DetailsForm, page, toastAndFollow, type Handler } from "../kit";
import type { FoundItem, Link } from "../types";

// HandInItem: the clerk follows a link with nothing filled in, and the feature answers NeedsDetails,
// which is shown as a form for what's missing. HandedIn says so, and goes back to the unclaimed items.

const needsDetails: Handler = (element, body: { Missing: string[]; LinkToHandIn: Link }, host) => page(element, <>
  <h1>Hand in an item</h1>
  <p className="lp-lede">Say what was found, and at which station.</p>
  <DetailsForm link={body.LinkToHandIn} missing={body.Missing} submit="Hand it in" host={host} fields={[
    { name: "Category", label: "What kind of thing, such as Umbrella" },
    { name: "Description", label: "Description" },
    { name: "Station", label: "Found at" }
  ]} />
</>);

const handedIn: Handler = (_, body: { Item: FoundItem; LinkToUnclaimedItems: Link }, host) =>
  toastAndFollow(host, `Handed in: ${body.Item.Description}.`, body.LinkToUnclaimedItems);

export const handInItem = { feature: "HandInItem", cases: { NeedsDetails: needsDetails, HandedIn: handedIn } };
