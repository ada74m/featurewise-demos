import type { Link, ServiceModule } from "./shell";

// The demos' module. It gives each demo's first page an address starting with the demo's name, as in
// "lost-property/unclaimed-items", where the book and the shell's default link to, and nothing else:
// every other query gets the shell's default address, and every result the shell's generic renderer,
// so the links appearing and disappearing are what a visitor sees.

const feature = (name: string) => `demos/${name}`;

const entrances: Record<string, string> = {
  "lost-property/unclaimed-items": feature("UnclaimedItems"),
  "box-office/performances": feature("Performances")
};

const demos: ServiceModule = {
  addressToLink(address) {
    const path = address.split("?")[0] ?? "";
    if (path === "" || path === "lost-property")
      return { $feature: feature("UnclaimedItems") };
    if (path === "box-office")
      return { $feature: feature("Performances") };
    return entrances[path] ? { $feature: entrances[path] } : null;
  },

  linkToAddress: (link: Link) =>
    Object.entries(entrances).find(([, named]) => named === link.$feature)?.[0] ?? null,

  render: () => null
};

export default demos;
