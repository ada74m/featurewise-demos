import type { Link, ServiceModule } from "./shell";

// The lost property office's module. It gives the office's first page the address "unclaimed-items",
// which the book links to, and nothing else: every other query gets the shell's default address, and
// every result the shell's generic renderer, so the links appearing and disappearing are what a visitor
// sees.

const unclaimedItems: Link = { $feature: "lost-property/UnclaimedItems" };

const lostProperty: ServiceModule = {
  addressToLink: address => {
    const path = address.split("?")[0] ?? "";
    return path === "" || path === "unclaimed-items" ? { ...unclaimedItems } : null;
  },

  linkToAddress: link => (link.$feature === unclaimedItems.$feature ? "unclaimed-items" : null),

  render: () => null
};

export default lostProperty;
