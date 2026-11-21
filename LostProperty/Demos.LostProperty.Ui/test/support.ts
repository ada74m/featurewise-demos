import { act } from "react";
import lostProperty from "../src/index";
import type { Host, Link, ShellResponse } from "@ada74m/featurewise-ui";

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

// A host that records what the module asks of the shell, and an element to render into.
export function setUp() {
  const followed: Link[] = [];
  const toasts: string[] = [];
  // Its paths are the module's addresses, as the shell's are once the module is loaded.
  const host: Host = {
    follow: link => followed.push(link),
    toast: message => toasts.push(message),
    embed: () => () => {},
    pathOf: link => {
      const address = lostProperty.linkToAddress(link);
      return address === null ? null : `/lost-property/${address}`;
    }
  };
  const element = document.createElement("div");
  document.body.replaceChildren(element);

  // Renders the response to following a link to the feature, as the shell would; null if the module
  // leaves it to the generic renderer.
  const render = (feature: string, body: unknown) => {
    const response: ShellResponse = { link: link(feature), feature: `lost-property/${feature}`, family: familyOf(feature), status: 200, contentType: "application/json", body };
    let cleared: (() => void) | null = null;
    act(() => { cleared = lostProperty.render(element, response, host); });
    return cleared;
  };

  const click = (text: string) => {
    const target = [...element.querySelectorAll("a, button")].find(node => node.textContent === text) as HTMLElement | undefined;
    if (!target)
      throw new Error(`nothing to click says ${text}`);
    act(() => target.click());
  };

  const type = (name: string, value: string) => {
    const input = element.querySelector(`input[name=${name}]`) as HTMLInputElement;
    act(() => {
      Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value")!.set!.call(input, value);
      input.dispatchEvent(new Event("input", { bubbles: true }));
    });
  };

  const submit = () => act(() => element.querySelector("form")!.requestSubmit());

  return { host, element, followed, toasts, render, click, type, submit };
}

// The office's queries; every other feature a test names is a command.
const QUERIES = ["UnclaimedItems", "ItemsFoundAt", "YourLostReports", "DonatedToCharity"];
const familyOf = (feature: string) => (QUERIES.includes(feature) ? "query" as const : "command" as const);

// A link to one of the office's features, named in the property its family uses.
export const link = (feature: string, args: Record<string, unknown> = {}): Link =>
  familyOf(feature) === "query"
    ? { $query: `lost-property/${feature}`, ...args }
    : { $command: `lost-property/${feature}`, ...args };

export const item = (id: string, description: string, station = "Crewe", status = "Unclaimed", claimedBy: string | null = null) =>
  ({ Id: id, Category: "Umbrella", Description: description, Station: station, HandedInAt: "2026-11-04T09:00:00Z", Status: status, ClaimedBy: claimedBy });

export const report = (reference: string, description: string) =>
  ({ Reference: reference, Traveller: "traveller", Category: "Umbrella", Description: description, ReportedAt: "2026-11-03T09:00:00Z" });
