import { addressToLink, linkToAddress, service } from "./addresses";
import { claimItem } from "./features/ClaimItem";
import { handInItem } from "./features/HandInItem";
import { reportLostItem } from "./features/ReportLostItem";
import { unclaimedItems } from "./features/UnclaimedItems";
import { yourLostReports } from "./features/YourLostReports";
import type { Handler } from "./kit";
import type { ServiceModule } from "./shell";

// The lost property office's module, dressing the office one feature at a time. Each feature's file
// holds its handlers, by result case. A result no handler takes, such as ItemsFoundAt's or
// DonatedToCharity's, is shown by the shell's generic renderer.

const dressed = [unclaimedItems, handInItem, yourLostReports, claimItem, reportLostItem];

const handlers = new Map<string, Handler>(dressed.flatMap(({ feature, cases }) =>
  Object.entries(cases).map(([name, handler]) => [`${service}/${feature}:${name}`, handler as Handler] as const)));

const lostProperty: ServiceModule = {
  addressToLink,
  linkToAddress,

  render(element, response, host) {
    const body = response.body as { $case?: string } | null;
    const handler = handlers.get(`${response.feature}:${body?.$case ?? ""}`);
    return handler ? handler(element, body as never, host) : null;
  }
};

export default lostProperty;
