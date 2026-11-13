import type { ModuleBuilder } from "demos-common-ui";

// ItemsFoundAt: an address only, such as found-at/York, so a station's items can be bookmarked or shared.
// Its result is left to the shell's generic renderer.

export default (office: ModuleBuilder) => office.addQuery("ItemsFoundAt")
  .address("found-at/{Station}");
