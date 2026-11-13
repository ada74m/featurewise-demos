import type { Renderer } from "demos-common-ui";
import { DetailsForm, page, toastAndFollow } from "../kit";

// HandInItem: the clerk follows a link with nothing filled in, and the feature answers NeedsDetails,
// which is shown as a form for what's missing. HandedIn says so, and goes back to the unclaimed items.

export const NeedsDetails: Renderer = (element, body, host) => page(element, <>
  <h1>Hand in an item</h1>
  <p className="lp-lede">Say what was found, and at which station.</p>
  <DetailsForm link={body.LinkToHandIn} missing={body.Missing} submit="Hand it in" host={host} fields={[
    { name: "Category", label: "What kind of thing, such as Umbrella" },
    { name: "Description", label: "Description" },
    { name: "Station", label: "Found at" }
  ]} />
</>);

export const HandedIn: Renderer = (_, body, host) =>
  toastAndFollow(host, `Handed in: ${body.Item.Description}.`, body.LinkToUnclaimedItems);
