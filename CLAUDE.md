# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```
export GITHUB_PACKAGES_TOKEN=$(gh auth token)   # restoring Featurewise's packages needs a token that can read packages
dotnet build                                     # build the solution (Demos.slnx)
dotnet test                                      # run all tests
dotnet run --project Demos.Local                 # run every demo locally; needs Demos.Ui and ../ui built
cd Demos.Ui && npm test && npm run build         # the shell module
```

A change is done when the build is warning-free and the full test run passes. For a behaviour change, temporarily remove the new code to confirm its test fails.

## What this is

The `demos` service: small live demos for the book, each in a micro-domain of its own, built as the `sample` service (`../sample`) is. **Read `../sample/PRINCIPLES.md` before changing code; the code follows it.** `README.md` describes the demos and how they're built. The plan for them, including the demos still to build, is "Step 4a" in `../book/code-plan.md`.

- **One namespace and set of folders per demo,** such as `Demos.LostProperty` in `Features/LostProperty`, `Models/LostProperty`, `Facilities/LostProperty` and `Events/LostProperty`. What every demo shares (`IChanges`, `IEvents`, `Sandboxes`, `Details`, `Service`) is in the `Demos` namespace.
- **The book's code samples come from here,** so keep the features readable on their own, and keep anything that isn't featurewise organisation (sandboxes above all) out of them.
- **Sandboxes are for facilities only.** `Sandboxes.Of(caller)` is read by the facilities' implementations (`SandboxedTables` in `Demos.Local`), and by no feature. Seeds are per demo, in the core's `Facilities/<Demo>/Seed.cs`, made the first time a sandbox uses a table.
- **Events go through the outbox:** a feature calls `IEvents.Raise`, and commits. `Demos.QueueWorker.Function.LinkFor` maps each event to its worker's link.
- **Every demo is shown through the shell's generic renderer.** `Demos.Ui` gives queries addresses, as `<demo>/<query>`, and handles no results.
- Personas are the local host's `devUsers` in `services.json` for now.

## Conventions

- One type per file.
- Feature names follow the families: queries are noun phrases, commands verb phrases, workers `Handle` plus their trigger.
- Prose (README, comments) is in classic style: direct, no metadiscourse, no hedging.
