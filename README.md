# Featurewise demos

Small, live demos of the ideas in *Featurewise Organisation*, each in a micro-domain of its own. They're one Featurewise service, `demos`, shown through the UI shell's generic renderer, so what a visitor sees is the features' own output: their result cases, and the links appearing and disappearing as the persona changes. The book's code samples come from here.

Built so far: the lost property office, for chapter 1.

## The lost property office (chapter 1)

A railway station's lost property office, showing a feature as a whole job, a command beside a query, both kinds of worker, result cases, and naming for each family.

| Feature | Family | What it does |
|---|---|---|
| `UnclaimedItems` | Query | Everything waiting to be claimed, oldest first |
| `ItemsFoundAt` | Query | What's waiting at one station |
| `YourLostReports` | Query | The caller's reports, each with any item that may be theirs, and a link to claim it |
| `DonatedToCharity` | Query | What the charity collections took |
| `HandInItem` | Command | A clerk logs a found item. Returns `HandedIn`, or `NeedsDetails` |
| `ReportLostItem` | Command | A traveller describes what they lost. Returns `Reported`, or `NeedsDetails` |
| `ClaimItem` | Command | `Claimed`, `AlreadyClaimed`, `DoesntMatchReport` or `NotFound` |
| `HandleItemHandedIn` | Worker, on the event `ItemHandedIn` | Notes the item on every unmatched report of the same kind of thing |
| `HandleCharityCollectionDue` | Worker, on a schedule | Gives away everything unclaimed for three months |

The personas are a traveller, who has already reported losing an umbrella, and a clerk. Hand in an umbrella as the clerk, switch to the traveller, and `YourLostReports` shows it, with a link to claim it.

## How it's built

As the `sample` service is (see its `PRINCIPLES.md`), with three additions:

- **A namespace and folders per demo:** `Demos.LostProperty`, in `Features/LostProperty`, `Models/LostProperty`, `Facilities/LostProperty` and `Events/LostProperty`.
- **Events, through an outbox.** `IEvents.Raise` stages the event in the invocation's `IChanges`, so it's committed with the feature's other writes or not at all. On AWS, the outbox is rows in the demos' table, and its stream delivers them to `Demos.QueueWorker`, which turns each event into its worker's link.
- **Sandboxes.** Each visitor works in a sandbox of their own, named by a `sandbox` claim in their token, which every persona they sign in as shares. Only the facilities read it (`SandboxedTables` locally, `DynamoTables` on AWS), so the features know nothing of it. Each demo seeds a sandbox the first time it's used, and on AWS a sandbox's data expires a week later. A scheduled job runs once in every sandbox in use, and an event is handled in the sandbox it was raised in.

| Project | What it is |
|---|---|
| `Demos` | The core: every demo's models, features, facilities and events. It references only `Featurewise.Core`. |
| `Demos.Aws` | The facilities on DynamoDB: one table for every demo, keyed by sandbox and `<table>#<key>`, each record a map in `Body` so conditions can test its fields; the unit of work (`DynamoChanges`, one `TransactWriteItems` per commit); the outbox; and the table's Terraform. |
| `Demos.UiBackend` | The backend for the UI shell: `POST /{feature}`, and `GET /{feature}?link=` for queries. |
| `Demos.ScheduledWorker` | Runs scheduled jobs, in every sandbox in use. |
| `Demos.QueueWorker` | Runs the workers that events trigger, from DynamoDB's stream. |
| `Demos.SignIn` | The demo sign-in: an OpenID Connect provider with no passwords, where a visitor becomes any persona in `personas.json`. The shell names one in `login_hint` and it's signed in at once; without one, the sign-in shows them all. Tokens are RS256, signed by a KMS key, and carry the visitor's sandbox, from a cookie the sign-in sets on the first visit. It stores nothing: a code is itself a signed token, good for two minutes and only with the PKCE verifier. |
| `Demos.Local` | Everything in one process, over facilities in memory. |
| `Demos.Ui` | The module for the UI shell: addresses for the queries, and nothing else. |
| `Demos.Tests` | The features, over memory and over DynamoDB Local, each entrypoint, and the local host. |

Each project keeps its Terraform in `Infrastructure/`, and the root `Infrastructure/` wires them together. `package.sh` builds the package an environment uses, and CI publishes it as `demos/<version>`.

`personas.json` lists who a visitor can be in each demo. The sign-in offers them, the local host's `services.json` does, and the environment passes them to the shell, whose persona bar shows a demo's personas above its pages. Any address takes `?as=<persona>`, as a link from the book does.

Not built yet: resetting a demo. See `code-plan.md` in the book's repo.

## Running locally

```
export GITHUB_PACKAGES_TOKEN=$(gh auth token)
(cd Demos.Ui && npm install && npm run build)
(cd ../ui && npm install && npm run build)        # the shell, from a sibling checkout of featurewise-ui
dotnet run --project Demos.Local
```

To run the DynamoDB tests too, start DynamoDB Local (`docker run -d -p 8000:8000 amazon/dynamodb-local`) and set `DYNAMODB_LOCAL=http://localhost:8000` before `dotnet test`; without it they're skipped.

Then open the URL it prints, sign in from the pill as the traveller or the clerk, and follow the links. Locally, events reach their workers as soon as they're committed, and the charity collects every minute.
