import { featurewiseModule } from "demos-common-ui";

// The lost property office's module, which each feature's file adds itself to. The service's own path
// leads to the unclaimed items.
export const lostProperty = featurewiseModule("lost-property", { home: "unclaimed-items" });
