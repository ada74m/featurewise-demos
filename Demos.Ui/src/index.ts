import type { Link, ServiceModule } from "./shell";

// The demos' module. It gives the demos' queries addresses, each starting with its demo's name, as in
// "lost-property/unclaimed-items", and handles no results itself: the demos show the shell's generic
// renderer, so the links appearing and disappearing are what a visitor sees.

const feature = (name: string) => `demos/${name}`;

// A query with no arguments has a fixed address.
const fixed: Record<string, string> = {
  "lost-property/unclaimed-items": feature("UnclaimedItems"),
  "lost-property/donated-to-charity": feature("DonatedToCharity"),
  "lost-property/your-lost-reports": feature("YourLostReports")
};

const foundAt = "lost-property/found-at/";

const demos: ServiceModule = {
  addressToLink(address) {
    const path = address.split("?")[0] ?? "";
    if (path === "" || path === "lost-property")
      return { $feature: feature("UnclaimedItems") };
    if (fixed[path])
      return { $feature: fixed[path] };
    if (path.startsWith(foundAt) && path.length > foundAt.length)
      return { $feature: feature("ItemsFoundAt"), Station: decodeURIComponent(path.slice(foundAt.length)) };
    return null;
  },

  linkToAddress(link: Link) {
    const address = Object.entries(fixed).find(([, named]) => named === link.$feature)?.[0];
    if (address)
      return address;
    if (link.$feature === feature("ItemsFoundAt") && typeof link.Station === "string")
      return foundAt + encodeURIComponent(link.Station);
    return null;
  },

  render: () => null
};

export default demos;
