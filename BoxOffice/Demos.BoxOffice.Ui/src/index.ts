import type { Link, ServiceModule } from "./shell";

// The box office's module. It gives the box office's first page the address "performances", and nothing
// else: every other query gets the shell's default address, and every result the shell's generic
// renderer.

const performances: Link = { $feature: "box-office/Performances" };

const boxOffice: ServiceModule = {
  addressToLink: address => {
    const path = address.split("?")[0] ?? "";
    return path === "" || path === "performances" ? { ...performances } : null;
  },

  linkToAddress: link => (link.$feature === performances.$feature ? "performances" : null),

  render: () => null
};

export default boxOffice;
