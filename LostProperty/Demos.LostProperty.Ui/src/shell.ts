// The shell's module contract, as featurewise-ui's src/contract.ts defines it. It's small and stable,
// so the module keeps its own copy of the types rather than depending on the shell's package.

export interface Link {
  $feature: string;
  [argument: string]: unknown;
}

export interface ShellResponse {
  link: Link;
  feature: string;
  status: number;
  contentType: string;
  body: unknown;
}

export interface Host {
  follow(link: Link): void;
  toast(message: string): void;
  embed(element: HTMLElement, link: Link, onResult?: (response: ShellResponse) => void): () => void;
}

export interface ServiceModule {
  addressToLink(address: string): Link | null;
  linkToAddress(link: Link): string | null;
  render(element: HTMLElement, response: ShellResponse, host: Host): (() => void) | null;
}
