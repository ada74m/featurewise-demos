// The shell's module contract, as featurewise-ui's src/contract.ts defines it. It's small and stable,
// so the demos keep their own copy of the types rather than depending on the shell's package.

export interface Link {
  $feature: string;
  [argument: string]: unknown;
}

export interface ShellResponse {
  link: Link;
  feature: string;
  family: string | null;
  status: number;
  contentType: string;
  body: unknown;
}

export interface Host {
  follow(link: Link): void;
  toast(message: string): void;
  embed(element: HTMLElement, link: Link, onResult?: (response: ShellResponse) => void): () => void;
  // The path in the shell where a link to a query loads, for an anchor's href, for a link to any service.
  pathOf(link: Link): string | null;
}

export interface ServiceModule {
  addressToLink(address: string): Link | null;
  linkToAddress(link: Link): string | null;
  render(element: HTMLElement, response: ShellResponse, host: Host): (() => void) | null;
}
