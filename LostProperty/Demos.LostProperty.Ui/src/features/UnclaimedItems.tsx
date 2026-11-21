import type { Renderer } from "@ada74m/featurewise-ui";
import { Anchor, Button, day, page } from "../kit";

// UnclaimedItems: everything waiting to be claimed, as a table. Each station is an anchor to what was
// found there, and the clerk, and only the clerk, is offered a button to hand something in.

export const result: Renderer = (element, body, host) => page(element, <>
  <h1>Unclaimed items</h1>
  <p className="lp-lede">Everything waiting to be claimed, oldest first.</p>
  <div className="fw-row lp-bar">
    {body.LinkToHandIn && <Button link={body.LinkToHandIn} host={host} primary>Hand in an item</Button>}
    <Anchor link={body.LinkToYourReports} host={host}>Your lost reports</Anchor>
    <Anchor link={body.LinkToDonated} host={host}>Donated to charity</Anchor>
  </div>
  {body.Items.length === 0
    ? <p className="lp-none">Nothing is waiting to be claimed.</p>
    : <table>
        <thead><tr><th>What</th><th>Found at</th><th>Handed in</th></tr></thead>
        <tbody>
          {body.Items.map(({ Item: item, LinkToStation: station }) => (
            <tr key={item.Id}>
              <td>{item.Description}<span className="lp-category">{item.Category}</span></td>
              <td><Anchor link={station} host={host}>{item.Station}</Anchor></td>
              <td>{day(item.HandedInAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>}
</>);
