import { Anchor, Button, DetailsForm, day, page, toastAndFollow, type Handler } from "../kit";

// ReportLostItem: a form for what's missing, then the reference. If the office already has something
// like it, the traveller is offered the claim straight away.

const needsDetails: Handler = (element, body, host) => page(element, <>
  <h1>Report something lost</h1>
  <p className="lp-lede">Tell us what you lost. If it's handed in, it'll appear on your reports.</p>
  <DetailsForm link={body.LinkToReport} missing={body.Missing} submit="Report it" host={host} fields={[
    { name: "Category", label: "What kind of thing, such as Umbrella" },
    { name: "Description", label: "Description" }
  ]} />
</>);

const reported: Handler = (_, body, host) =>
  toastAndFollow(host, `Reported. Your reference is ${body.Report.Reference}.`, body.LinkToYourReports);

const mayAlreadyHaveIt: Handler = (element, body, host) =>
  page(element, <>
    <h1>We may already have it</h1>
    <div className="lp-notice">
      <p>Your reference is {body.Report.Reference}.</p>
      <p>{body.Item.Description} was handed in at {body.Item.Station} on {day(body.Item.HandedInAt)}.</p>
    </div>
    <div className="lp-bar">
      <Button link={body.LinkToClaim} host={host} primary>Claim it</Button>
      <Anchor link={body.LinkToYourReports} host={host}>Not mine: see your reports</Anchor>
    </div>
  </>);

export const reportLostItem = {
  feature: "ReportLostItem",
  cases: { NeedsDetails: needsDetails, Reported: reported, MayAlreadyHaveIt: mayAlreadyHaveIt }
};
