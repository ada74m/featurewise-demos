import { Anchor, DetailsForm, page, toastAndFollow, type Handler } from "../kit";
import type { FoundItem, Link } from "../types";

// ClaimItem: Claimed says where to collect the item, and goes to the traveller's reports. A reference
// that doesn't match is a way forward, not a dead end: a form to try another, and the reports where the
// references are. The other cases say what happened.

const claimed: Handler = (_, body: { Item: FoundItem; LinkToYourReports: Link }, host) =>
  toastAndFollow(host, `Claimed: ${body.Item.Description}. Collect it from ${body.Item.Station}.`, body.LinkToYourReports);

const doesntMatchReport: Handler = (element, body: { Reference: string; LinkToClaim: Link; LinkToYourReports: Link }, host) =>
  page(element, <>
    <h1>That reference doesn't match</h1>
    <p className="lp-lede">
      {body.Reference} isn't the reference of a report this item matches. Your references are
      on <Anchor link={body.LinkToYourReports} host={host}>your reports</Anchor>.
    </p>
    <DetailsForm link={body.LinkToClaim} missing={[]} submit="Claim it" host={host}
                 fields={[{ name: "Reference", label: "Your reference" }]} />
  </>);

const alreadyClaimed: Handler = element => page(element, <>
  <h1>Already claimed</h1>
  <p className="lp-lede">Someone has already claimed that item.</p>
</>);

const notFound: Handler = (element, body: { ItemId: string }) => page(element, <>
  <h1>No such item</h1>
  <p className="lp-lede">There's no item {body.ItemId}.</p>
</>);

export const claimItem = {
  feature: "ClaimItem",
  cases: { Claimed: claimed, DoesntMatchReport: doesntMatchReport, AlreadyClaimed: alreadyClaimed, NotFound: notFound }
};
