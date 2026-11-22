# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```
export GITHUB_PACKAGES_TOKEN=$(gh auth token)   # restoring Featurewise's packages needs a token that can read packages
dotnet build                                     # build the solution (Demos.slnx)
dotnet test                                      # run all tests; the DynamoDB ones need DYNAMODB_LOCAL=http://localhost:8000
                                                 # and docker run -d -p 8000:8000 amazon/dynamodb-local, or they're skipped
dotnet run --project Demos.Local                 # run every demo locally; needs each module and ../ui built
cd LostProperty/Demos.LostProperty.Ui && npm test && npm run build   # a demo's module
./package.sh <version> out                       # build the packages: lost-property, box-office and sign-in
terraform fmt -check -recursive .
```

A change is done when the build is warning-free and the full test run passes, DynamoDB's included. Never `plan` or `apply` against AWS unasked. For a behaviour change, temporarily remove the new code to confirm its test fails.

## What this is

Small live demos for the book, each a featurewise service in a micro-domain of its own, built as the `sample` service (`../sample`) is. **Read `../sample/PRINCIPLES.md` before changing code; the code follows it.** `README.md` describes the demos and how they're built. The plan for them, including the demos still to build, is "Step 4a" in the book's `code-plan.md` (`../book/code-plan.md`, where the book's private repository is checked out beside this one).

- **Each demo is a service of its own,** in a folder of its own ("Splitting the demos" in the book's `code-plan.md`, where present): `LostProperty/` is `lost-property`, `BoxOffice/` is `box-office`. Each has its core `Demos.<Demo>` (`Features`, `Models`, `Facilities`, `Events`, `Service`), `.Aws` and `.Local` facilities, `.Functions` (one zip, a handler per function), `.Ui`, `.Tests`, `Infrastructure/` and `demo.json`. No demo references another's projects. What every demo shares is in `Common/`: `Demos.Common` (`IChanges`, `IEvents`, `IDemoData`, `Sandboxes`, `Details`, in the `Demos` namespace), the plumbing on AWS and in memory, the functions' shared handlers (`Demos.Functions`), `ResetDemo`, the tests' fakes (`Demos.Common.Testing`), and the Terraform modules.
- **The book's code samples come from here,** so keep the features readable on their own, and keep anything that isn't featurewise organisation (sandboxes above all) out of them.
- **Sandboxes are for facilities only.** `Sandboxes.Of(caller)` is read by the facilities' implementations (`SandboxedTables` in `Demos.Common.Local`, `DynamoTables` in `Demos.Common.Aws`), and by no feature. Seeds are per demo, in its core's `Facilities`, made the first time a sandbox uses a table.
- **Events go through the outbox:** a feature calls `IEvents.Raise`, and commits. A demo's `QueueWorker` function maps each event to its worker's link.
- **Demos start in the shell's generic renderer, and some are dressed.** Every module is built with `featurewiseModule` from the shell's npm package, `@ada74m/featurewise-ui` (the builder and the contract's types, published by `../ui` to the owner's GitHub Packages registry as `0.1.0-preview.<run>`; each module's `.npmrc` names the registry, and restoring needs `GITHUB_PACKAGES_TOKEN`, as the .NET packages do; to try a change to the builder before it's published, point a module's dependency at `file:../../../ui`): `addQuery(feature).address("found-at/{Station}").render(…)`, `addCommand(feature).renderCase(name, …)`; what isn't added is left to the shell's defaults. An address template reads and writes a link both ways, and is used only for a link whose arguments (not `CausedBy`) it holds exactly. A result's body is untyped (`any`): its shape is the feature's. The box office's module gives its first page an address and renders nothing. The lost property office's (chapter 5) is dressed feature by feature, in React as the sample's is: a file per feature in `src/features/` exporting its renderers, each named after its result case (`result` for a result without cases), and all the wiring in `src/index.tsx`: every feature, its address and its renderers in one list. `ItemsFoundAt` (an address only) and `DonatedToCharity` (absent) are deliberately left generic. The UI module itself is called `ui`, made in the index. Its look is the shell's (`../ui/README.md`, "Styles"): plain elements come styled, `fw-stack` and `fw-row` lay things out, and `src/styles.ts` holds only what's the office's own, scoped under `.lp` and built from the shell's `--fw-*` properties. The kit's `Anchor` takes its href from the shell's `host.pathOf(link)`, which knows every service's addresses and its own default ones. Every other query has the shell's default address, `_/<Feature>?link=…`, and the shell's services page can turn a module's rendering off. Persona ids are unique across demos, since the sign-in and `?as=` name a persona by its id.
- **On AWS** each service has one table (`Common/Infrastructure/modules/table`): partition key the sandbox, sort key `<table>#<key>`, the record as a map in `Body`, TTL on `Expires`. A facility's conditions test the record's fields (`DynamoCondition`, joined with `And`). The outbox is rows under `outbox#`, which the stream delivers to the queue worker. `LostPropertyTests` and `BoxOfficeTests` run against both implementations; a new demo's should too. Only DynamoDB's run concurrently, so a race test proves a condition there and not in memory.
- **Each demo is described in its `demo.json`:** its id (its service's id), title, chapter, text (`about`, `storySoFar`, `tryThis`, plain paragraphs in which persona names become buttons), personas (id, label, groups) and reset link. A demo's rules see a persona's id as the caller's name and its groups as roles. The local host serves it in `services.json`; on AWS, the service's module outputs it as `demo`, and the environment passes every demo's to the sign-in (`SignIn/`) and the shell, which shows it beside the demo's pages, links to it from the bar across the top, and lists it on the home page. Keep `storySoFar` true to the demo's seed.
- **`ResetDemo`**, which every demo registers, forgets everything its service holds in the caller's sandbox (`IDemoData`): locally by dropping its tables, on DynamoDB by deleting its tables' markers, so the next use seeds a new generation and older rows are never read.
- **The demo sign-in is an identity provider, not a featurewise service** (`SignIn/README.md`). It stores nothing: codes are signed tokens, and the sandbox is a cookie on its own domain, copied into every token's `sandbox` claim. The environment makes it with the shell's URL, and every demo's UI backend trusts only it.

## Conventions

- One type per file.
- Feature names follow the families: queries are noun phrases, commands verb phrases, workers `Handle` plus their trigger.
- Prose (README, comments) is in classic style: direct, no metadiscourse, no hedging.
