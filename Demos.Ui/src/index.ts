import type { Link, ServiceModule } from "./shell";

// The box office's module, still the demos service's until the box office becomes a service of its own.
// It gives the box office's first page the address "box-office/performances", and nothing else: every
// other query gets the shell's default address, and every result the shell's generic renderer.

const performances: Link = { $feature: "demos/Performances" };

const demos: ServiceModule = {
  addressToLink(address) {
    const path = address.split("?")[0] ?? "";
    return path === "box-office" || path === "box-office/performances" ? { ...performances } : null;
  },

  linkToAddress: link => (link.$feature === performances.$feature ? "box-office/performances" : null),

  render: () => null
};

export default demos;
