import type { Link } from "./shell";

// The office's addresses: readable ones for the pages worth bookmarking or sharing. Every other query
// gets the shell's default address, and no command gets one at all.

export const service = "lost-property";

const feature = (name: string) => `${service}/${name}`;
const foundAt = "found-at/";

export function addressToLink(address: string): Link | null {
  const path = address.split("?")[0] ?? "";
  if (path === "" || path === "unclaimed-items")
    return { $feature: feature("UnclaimedItems") };
  if (path === "your-reports")
    return { $feature: feature("YourLostReports") };
  if (path.startsWith(foundAt) && path.length > foundAt.length)
    return { $feature: feature("ItemsFoundAt"), Station: decodeURIComponent(path.slice(foundAt.length)) };
  return null;
}

export function linkToAddress(link: Link): string | null {
  switch (link.$feature) {
    case feature("UnclaimedItems"):
      return "unclaimed-items";
    case feature("YourLostReports"):
      return "your-reports";
    case feature("ItemsFoundAt"):
      return typeof link.Station === "string" ? foundAt + encodeURIComponent(link.Station) : null;
    default:
      return null;
  }
}
