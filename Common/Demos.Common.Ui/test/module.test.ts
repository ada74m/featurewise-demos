import { describe, expect, it } from "vitest";
import { featurewiseModule, type Host, type Renderer, type ShellResponse } from "../src/index";

const host: Host = { follow: () => {}, toast: () => {}, embed: () => () => {} };
const element = {} as HTMLElement;

const response = (feature: string, body: unknown): ShellResponse =>
  ({ link: { $feature: feature }, feature, family: "command", status: 200, contentType: "application/json", body });

// A renderer that records what it was given.
function recording() {
  const seen: unknown[] = [];
  const renderer: Renderer = (_, body) => {
    seen.push(body);
    return () => {};
  };
  return { seen, renderer };
}

describe("a module's addresses", () => {
  const module = featurewiseModule("shop", { home: "aisles" });
  module.addQuery("Aisles").address("aisles");
  module.addQuery("ShelvesIn").address("aisles/{Aisle}/shelves");
  module.addQuery("Shelf").address("aisles/{Aisle}/shelves/{Number:int}");
  module.addQuery("DeliveriesOn").address("deliveries/{On:date}");
  module.addQuery("Basket");
  module.addCommand("BuyThing");

  it("lead from a query's link to its address and back", () => {
    for (const [address, link] of [
      ["aisles", { $feature: "shop/Aisles" }],
      ["aisles/Tins%20and%20jars/shelves", { $feature: "shop/ShelvesIn", Aisle: "Tins and jars" }],
      ["aisles/Bakery/shelves/3", { $feature: "shop/Shelf", Aisle: "Bakery", Number: 3 }],
      ["deliveries/2026-11-13", { $feature: "shop/DeliveriesOn", On: "2026-11-13" }]
    ] as const) {
      expect(module.linkToAddress(link)).toBe(address);
      expect(module.addressToLink(address)).toEqual(link);
    }
  });

  it("take the home address for the service's own path, and ignore a query string", () => {
    expect(module.addressToLink("")).toEqual({ $feature: "shop/Aisles" });
    expect(module.addressToLink("aisles?utm=book")).toEqual({ $feature: "shop/Aisles" });
  });

  it("give the path in the shell", () => {
    expect(module.pathOf({ $feature: "shop/ShelvesIn", Aisle: "Bakery" })).toBe("/shop/aisles/Bakery/shelves");
    expect(module.pathOf({ $feature: "shop/Basket" })).toBeNull();
  });

  it("read only what fits the template's kinds", () => {
    expect(module.addressToLink("aisles/Bakery/shelves/three")).toBeNull();
    expect(module.addressToLink("aisles/Bakery/shelves/3.5")).toBeNull();
    expect(module.addressToLink("deliveries/Friday")).toBeNull();
    expect(module.addressToLink("aisles//shelves")).toBeNull();
    expect(module.addressToLink("nowhere")).toBeNull();
  });

  it("leave out the invocation that made the link, which isn't one of its arguments", () => {
    expect(module.linkToAddress({ $feature: "shop/ShelvesIn", Aisle: "Bakery", CausedBy: "00-4bf9-00f0-01" })).toBe("aisles/Bakery/shelves");
  });

  it("leave a link to the shell's default unless the address holds exactly its arguments", () => {
    expect(module.linkToAddress({ $feature: "shop/ShelvesIn", Aisle: "Bakery", Extra: 1 })).toBeNull();
    expect(module.linkToAddress({ $feature: "shop/ShelvesIn" })).toBeNull();
    expect(module.linkToAddress({ $feature: "shop/ShelvesIn", Aisle: null })).toBeNull();
    expect(module.linkToAddress({ $feature: "shop/Shelf", Aisle: "Bakery", Number: "3" })).toBeNull();
  });

  it("are given only to the queries that ask for one", () => {
    expect(module.linkToAddress({ $feature: "shop/Basket" })).toBeNull();
    expect(module.linkToAddress({ $feature: "shop/BuyThing" })).toBeNull();
    expect("address" in module.addCommand("ReturnThing")).toBe(false);
  });

  it("refuse templates that couldn't be read back", () => {
    const other = featurewiseModule("other");
    expect(() => other.addQuery("A").address("a/{X}/{X}")).toThrow();
    expect(() => other.addQuery("B").address("b-{X}")).toThrow();
    expect(() => other.addQuery("C").address("_/c")).toThrow();
    other.addQuery("D").address("d/{X}");
    expect(() => other.addQuery("E").address("d/{Y}")).toThrow();
  });
});

describe("a module's rendering", () => {
  it("renders a result by its feature, and by its case if it has one", () => {
    const module = featurewiseModule("shop");
    const aisles = recording();
    const bought = recording();
    module.addQuery("Aisles").render(aisles.renderer);
    module.addCommand("BuyThing").renderCase("Bought", bought.renderer);

    expect(module.render(element, response("shop/Aisles", { Aisles: [] }), host)).not.toBeNull();
    expect(module.render(element, response("shop/BuyThing", { $case: "Bought", Thing: "Tea" }), host)).not.toBeNull();
    expect(aisles.seen).toEqual([{ Aisles: [] }]);
    expect(bought.seen).toEqual([{ $case: "Bought", Thing: "Tea" }]);
  });

  it("leaves every feature and case it wasn't given to the generic renderer", () => {
    const module = featurewiseModule("shop");
    module.addCommand("BuyThing").renderCase("Bought", recording().renderer);

    expect(module.render(element, response("shop/BuyThing", { $case: "OutOfStock" }), host)).toBeNull();
    expect(module.render(element, response("shop/Basket", {}), host)).toBeNull();
    expect(module.render(element, response("elsewhere/BuyThing", { $case: "Bought" }), host)).toBeNull();
  });

  it("refuses a feature, or a case, added twice", () => {
    const module = featurewiseModule("shop");
    const buy = module.addCommand("BuyThing").renderCase("Bought", recording().renderer);
    expect(() => buy.renderCase("Bought", recording().renderer)).toThrow();
    expect(() => module.addQuery("BuyThing")).toThrow();
  });
});
