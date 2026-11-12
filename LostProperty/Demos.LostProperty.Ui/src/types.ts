import type { Link } from "./shell";

// The office's records, as its features return them.

export interface FoundItem {
  Id: string;
  Category: string;
  Description: string;
  Station: string;
  HandedInAt: string;
  Status: "Unclaimed" | "Claimed" | "DonatedToCharity";
  ClaimedBy?: string | null;
}

export interface LostReport {
  Reference: string;
  Traveller: string;
  Category: string;
  Description: string;
  ReportedAt: string;
  MatchedItemId?: string | null;
}

// A result as the module receives it: its case, if it has one, and its properties, links among them.
export type Body = { $case?: string; [property: string]: unknown };

export type { Link };
