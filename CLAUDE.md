# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```
export GITHUB_PACKAGES_TOKEN=$(gh auth token)   # restoring Featurewise's packages needs a token that can read packages
dotnet build                                     # build the solution (Demos.slnx)
dotnet test                                      # run all tests; the DynamoDB ones need DYNAMODB_LOCAL=http://localhost:8000
                                                 # and docker run -d -p 8000:8000 amazon/dynamodb-local, or they're skipped
dotnet run --project Demos.Local                 # run every demo locally; needs Demos.Ui and ../ui built
cd Demos.Ui && npm test && npm run build         # the shell module
./package.sh <version> out                       # build the package
terraform fmt -check -recursive .
```

A change is done when the build is warning-free and the full test run passes, DynamoDB's included. Never `plan` or `apply` against AWS unasked. For a behaviour change, temporarily remove the new code to confirm its test fails.

## What this is

The `demos` service: small live demos for the book, each in a micro-domain of its own, built as the `sample` service (`../sample`) is. **Read `../sample/PRINCIPLES.md` before changing code; the code follows it.** `README.md` describes the demos and how they're built. The plan for them, including the demos still to build, is "Step 4a" in `../book/code-plan.md`.

- **One namespace and set of folders per demo,** such as `Demos.LostProperty` in `Features/LostProperty`, `Models/LostProperty`, `Facilities/LostProperty` and `Events/LostProperty`. What every demo shares (`IChanges`, `IEvents`, `Sandboxes`, `Details`, `Service`) is in the `Demos` namespace.
- **The book's code samples come from here,** so keep the features readable on their own, and keep anything that isn't featurewise organisation (sandboxes above all) out of them.
- **Sandboxes are for facilities only.** `Sandboxes.Of(caller)` is read by the facilities' implementations (`SandboxedTables` in `Demos.Local`, `DynamoTables` in `Demos.Aws`), and by no feature. Seeds are per demo, in the core's `Facilities/<Demo>/Seed.cs`, made the first time a sandbox uses a table.
- **Events go through the outbox:** a feature calls `IEvents.Raise`, and commits. `Demos.QueueWorker.Function.LinkFor` maps each event to its worker's link.
- **Every demo is shown through the shell's generic renderer.** `Demos.Ui` gives queries addresses, as `<demo>/<query>`, and handles no results.
- **On AWS** every demo shares one table (`Demos.Aws/Infrastructure`): partition key the sandbox, sort key `<table>#<key>`, the record as a map in `Body`, TTL on `Expires`. A facility's conditions test the record's fields (`DynamoCondition`). The outbox is rows under `outbox#`, which the stream delivers to the queue worker. `LostPropertyTests` runs against both implementations; a new demo's facility tests should too.
- **Personas are in `personas.json`:** id, label, demo and groups. A demo's rules see the id as the caller's name and the groups as roles. The local host serves them in `services.json`, the sign-in (`Demos.SignIn`) offers them, and the module's `personas` output gives them to the shell.
- **The demo sign-in** stores nothing: codes are signed tokens, and the sandbox is a cookie on its own domain, copied into every token's `sandbox` claim. The root module takes `shell_url` and makes it, and the UI backend trusts only it.

## Conventions

- One type per file.
- Feature names follow the families: queries are noun phrases, commands verb phrases, workers `Handle` plus their trigger.
- Prose (README, comments) is in classic style: direct, no metadiscourse, no hedging.
