import { featurewiseModule } from "demos-common-ui";

// The box office's UI module. It gives the box office's first page the address "performances", and nothing
// else: every other query gets the shell's default address, and every result the shell's generic
// renderer.

const ui = featurewiseModule("box-office", { home: "performances" });

ui.addQuery("Performances").address("performances");

export default ui;
