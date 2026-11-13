import type { Renderer } from "demos-common-ui";
import { Anchor, Button, day, page } from "../kit";

// YourLostReports: the traveller's reports, each with whatever the office has noticed may be theirs.
// A match they can claim comes with a button; the feature leaves the link out when they can't.

function Match({ entry, host }) {
  const { Report: report, PossibleMatch: item, LinkToClaim: claim } = entry;
  if (!item)
    return <p className="lp-none">Nothing like it has been handed in yet.</p>;
  if (claim)
    return (
      <div className="lp-match">
        <p>We may have it: {item.Description}, handed in at {item.Station} on {day(item.HandedInAt)}.</p>
        <Button link={claim} host={host} primary>Claim it</Button>
      </div>
    );
  return (
    <p className="lp-none">
      {item.Status === "Claimed" && item.ClaimedBy === report.Traveller ? `You claimed it: ${item.Description}.`
        : item.Status === "DonatedToCharity" ? `${item.Description} went to charity.`
        : `${item.Description} was claimed by someone else.`}
    </p>
  );
}

export const result: Renderer = (element, body, host) => page(element, <>
  <h1>Your lost reports</h1>
  <p className="lp-lede">What you've told us you lost, and anything handed in that may be yours.</p>
  <div className="lp-bar">
    <Button link={body.LinkToReport} host={host} primary>Report something lost</Button>
    <Anchor link={body.LinkToUnclaimedItems} host={host}>Unclaimed items</Anchor>
  </div>
  {body.Reports.length === 0
    ? <p className="lp-none">You haven't reported anything lost.</p>
    : <div className="lp-cards">
        {body.Reports.map(entry => (
          <section key={entry.Report.Reference} className="lp-card">
            <h2>{entry.Report.Description}</h2>
            <span className="lp-category">{entry.Report.Category} · reported {day(entry.Report.ReportedAt)} · reference {entry.Report.Reference}</span>
            <Match entry={entry} host={host} />
          </section>
        ))}
      </div>}
</>);
