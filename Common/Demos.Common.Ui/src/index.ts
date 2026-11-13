// What every demo's module shares: the shell's contract, and the builder that makes a module from its
// features.
export { featurewiseModule } from "./module";
export type { CommandBuilder, ModuleBuilder, ModuleOptions, QueryBuilder, Renderer } from "./module";
export type { Host, Link, ServiceModule, ShellResponse } from "./shell";
