import type { Renderer } from "@ada74m/featurewise-ui";
import { Anchor, DetailsForm, page, toastAndFollow } from "../kit";

// ClaimItem: Claimed says where to collect the item, and goes to the traveller's reports. A reference
// that doesn't match is a way forward, not a dead end: a form to try another, and the reports where the
// references are. The other cases say what happened.

export const Claimed: Renderer = (_, body, host) =>
  toastAndFollow(host, `Claimed: ${body.Item.Description}. Collect it from ${body.Item.Station}.`, body.LinkToYourReports);

export const DoesntMatchReport: Renderer = (element, body, host) =>
  page(element, <>
    <h1>That reference doesn't match</h1>
    <p className="lp-lede">
      {body.Reference} isn't the reference of a report this item matches. Your references are
      on <Anchor link={body.LinkToYourReports} host={host}>your reports</Anchor>.
    </p>
    <DetailsForm link={body.LinkToClaim} missing={[]} submit="Claim it" host={host}
                 fields={[{ name: "Reference", label: "Your reference" }]} />
  </>);

export const AlreadyClaimed: Renderer = element => page(element, <>
  <h1>Already claimed</h1>
  <p className="lp-lede">Someone has already claimed that item.</p>
</>);

export const NotFound: Renderer = (element, body) => page(element, <>
  <h1>No such item</h1>
  <p className="lp-lede">There's no item {body.ItemId}.</p>
</>);
