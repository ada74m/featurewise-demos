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
cd Demos.Ui && npm test && npm run build         # the box office's module, until it moves
./package.sh <version> out                       # build the package
terraform fmt -check -recursive .
```

A change is done when the build is warning-free and the full test run passes, DynamoDB's included. Never `plan` or `apply` against AWS unasked. For a behaviour change, temporarily remove the new code to confirm its test fails.

## What this is

The `demos` service: small live demos for the book, each in a micro-domain of its own, built as the `sample` service (`../sample`) is. **Read `../sample/PRINCIPLES.md` before changing code; the code follows it.** `README.md` describes the demos and how they're built. The plan for them, including the demos still to build, is "Step 4a" in `../book/code-plan.md`.

- **Each demo is becoming a service of its own** ("Splitting the demos" in `../book/code-plan.md`). The lost property office is one: `LostProperty/`, the service `lost-property`, with its core `Demos.LostProperty` (`Features`, `Models`, `Facilities`, `Events`, `Service`), its facilities, functions, module, tests, Terraform and `demo.json`. The box office is still in `Demos*`, the service `demos`. What every demo shares is in `Common/`: `Demos.Common` (`IChanges`, `IEvents`, `IDemoData`, `Sandboxes`, `Details`, in the `Demos` namespace), the plumbing on AWS and in memory, the functions' shared handlers (`Demos.Functions`), `ResetDemo`, the tests' fakes (`Demos.Common.Testing`), and the Terraform modules.
- **The book's code samples come from here,** so keep the features readable on their own, and keep anything that isn't featurewise organisation (sandboxes above all) out of them.
- **Sandboxes are for facilities only.** `Sandboxes.Of(caller)` is read by the facilities' implementations (`SandboxedTables` in `Demos.Local`, `DynamoTables` in `Demos.Aws`), and by no feature. Seeds are per demo, in its core's `Facilities`, made the first time a sandbox uses a table.
- **Events go through the outbox:** a feature calls `IEvents.Raise`, and commits. A demo's `QueueWorker` function maps each event to its worker's link.
- **Every demo is shown through the shell's generic renderer.** Each demo's module gives its first page an address, such as `/lost-property/unclaimed-items`, and handles no results; every other query has the shell's default address, `_/<Feature>?link=…`. The shell's demo panel tells the demo from the signed-in persona, so persona ids are unique across demos.
- **On AWS** each service has one table (`Common/Infrastructure/modules/table`): partition key the sandbox, sort key `<table>#<key>`, the record as a map in `Body`, TTL on `Expires`. A facility's conditions test the record's fields (`DynamoCondition`, joined with `And`). The outbox is rows under `outbox#`, which the stream delivers to the queue worker. `LostPropertyTests` and `BoxOfficeTests` run against both implementations; a new demo's should too. Only DynamoDB's run concurrently, so a race test proves a condition there and not in memory.
- **Each demo is described in its `demo.json`** (the box office's still in `demos.json`): its id (its service's id), title, chapter, text (`about`, `storySoFar`, `tryThis`, plain paragraphs in which persona names become buttons), personas (id, label, groups) and reset link. A demo's rules see a persona's id as the caller's name and its groups as roles. The local host serves the file in `services.json`, the root module passes its personas to the sign-in (`SignIn/`), and the module's `demos` output gives it to the shell, which shows it above the demo's pages. Keep `storySoFar` true to the demo's seed.
- **`ResetDemo`**, which every demo registers, forgets everything its service holds in the caller's sandbox (`IDemoData`): locally by dropping its tables, on DynamoDB by deleting its tables' markers, so the next use seeds a new generation and older rows are never read.
- **The demo sign-in is an identity provider, not a featurewise service** (`SignIn/README.md`). It stores nothing: codes are signed tokens, and the sandbox is a cookie on its own domain, copied into every token's `sandbox` claim. The root module takes `shell_url` and makes it, and the UI backend trusts only it.

## Conventions

- One type per file.
- Feature names follow the families: queries are noun phrases, commands verb phrases, workers `Handle` plus their trigger.
- Prose (README, comments) is in classic style: direct, no metadiscourse, no hedging.
