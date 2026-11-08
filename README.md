# Featurewise demos

Small, live demos of the ideas in *Featurewise Organisation*, each in a micro-domain of its own. They're one Featurewise service, `demos`, shown through the UI shell's generic renderer, so what a visitor sees is the features' own output: their result cases, and the links appearing and disappearing as the persona changes. The book's code samples come from here.

Built so far: the lost property office, for chapters 1 and 3, and the theatre box office, for chapters 5 and 6.

## The lost property office (chapters 1 and 3)

A railway station's lost property office, showing a feature as a whole job, a command beside a query, both kinds of worker, result cases, and naming for each family.

| Feature | Family | What it does |
|---|---|---|
| `UnclaimedItems` | Query | Everything waiting to be claimed, oldest first |
| `ItemsFoundAt` | Query | What's waiting at one station |
| `YourLostReports` | Query | The caller's reports, each with any item that may be theirs, and a link to claim it |
| `DonatedToCharity` | Query | What the charity collections took |
| `HandInItem` | Command | A clerk logs a found item. Returns `HandedIn`, or `NeedsDetails` with the link back to complete |
| `ReportLostItem` | Command | A traveller describes what they lost. Returns `Reported`; `MayAlreadyHaveIt`, with a link to claim the item, if the office already has something like it; or `NeedsDetails` with the link back to complete |
| `ClaimItem` | Command | `Claimed`, `AlreadyClaimed`, `DoesntMatchReport` (with the link back to try another reference, and one to the traveller's reports) or `NotFound` |
| `HandleItemHandedIn` | Worker, on the event `ItemHandedIn` | Notes the item on every report still looking for the same kind of thing |
| `HandleCharityCollectionDue` | Worker, on a schedule | Gives away everything unclaimed for three months |

The personas are a traveller, who has already reported losing an umbrella, and a clerk. Hand in an umbrella as the clerk, switch to the traveller, and `YourLostReports` shows it, with a link to claim it. Matching works in either order: a report of something already handed in is matched as it's made. Both `HandleItemHandedIn` and `ReportLostItem` apply the rule in `Matches`, under which a report is still looking until its traveller claims its match, so a report whose match went to someone else or to charity is matched again.

## The theatre box office (chapters 5 and 6)

A small theatre's box office, selling seats for the four performances of *The Pirates of Penge*. It shows links offered only when they'd work, by the state of things and by who's asking, links that bring the next step to you, and result cases that turn an obstacle into a way forward.

| Feature | Family | What it does |
|---|---|---|
| `Performances` | Query | Every performance, each with `LinkToBuySeats` while it has seats, `LinkToJoinReturnsQueue` once it's sold out, and for staff, `LinkToCancel` and `LinkToReleaseHouseSeats` |
| `YourTickets` | Query | The caller's tickets, and the performances they're waiting for returns to, each with a link to buy seats once there are some |
| `BuySeats` | Command | `Bought`, `NeedsDetails` (with the link back to complete), `NotEnoughSeats` (with a link to buy those left), `SoldOut` (with a link to the returns queue), `PerformanceCancelled` or `NotFound` |
| `JoinReturnsQueue` | Command | `Joined`, `AlreadyQueuing`, `SeatsAvailable` (with a link to buy them), `PerformanceCancelled` or `NotFound` |
| `CancelPerformance` | Command | Staff only: `Cancelled`, `AlreadyCancelled` or `NotFound` |
| `ReleaseHouseSeats` | Command | Staff only: puts the seats held back for the house on sale. `Released`, `NoHouseSeats`, `PerformanceCancelled` or `NotFound` |

The personas are a theatregoer, who already has tickets for Thursday, and box office staff. The Saturday matinee is sold out: join its returns queue as the theatregoer, release its house seats as staff, and the theatregoer's place in the queue offers a link to buy them. A performance changes only if nobody has changed it since it was read, so two tabs buying the last seats can't both have them.

## How it's built

As the `sample` service is (see its `PRINCIPLES.md`), with three additions:

- **A namespace and folders per demo,** such as `Demos.LostProperty`, in `Features/LostProperty`, `Models/LostProperty`, `Facilities/LostProperty` and `Events/LostProperty`.
- **Events, through an outbox.** `IEvents.Raise` stages the event in the invocation's `IChanges`, so it's committed with the feature's other writes or not at all. On AWS, the outbox is rows in the demos' table, and its stream delivers them to `Demos.QueueWorker`, which turns each event into its worker's link.
- **Sandboxes.** Each visitor works in a sandbox of their own, named by a `sandbox` claim in their token, which every persona they sign in as shares. Only the facilities read it (`SandboxedTables` locally, `DynamoTables` on AWS), so the features know nothing of it. Each demo seeds a sandbox the first time it's used, and on AWS a sandbox's data expires a week later. A scheduled job runs once in every sandbox in use, and an event is handled in the sandbox it was raised in.

| Project | What it is |
|---|---|
| `Demos` | The core: every demo's models, features, facilities and events. It references only `Featurewise.Core`. |
| `Demos.Aws` | The facilities on DynamoDB: one table for every demo, keyed by sandbox and `<table>#<key>`, each record a map in `Body` so conditions can test its fields; the unit of work (`DynamoChanges`, one `TransactWriteItems` per commit); the outbox; and the table's Terraform. |
| `Demos.UiBackend` | The backend for the UI shell: `POST /{feature}`, and `GET /{feature}?link=` for queries. |
| `Demos.ScheduledWorker` | Runs scheduled jobs, in every sandbox in use. |
| `Demos.QueueWorker` | Runs the workers that events trigger, from DynamoDB's stream. |
| `Demos.SignIn` | The demo sign-in: an OpenID Connect provider with no passwords, where a visitor becomes any persona in `demos.json`. The shell names one in `login_hint` and it's signed in at once; without one, the sign-in shows them all. Tokens are RS256, signed by a KMS key, and carry the visitor's sandbox, from a cookie the sign-in sets on the first visit. It stores nothing: a code is itself a signed token, good for two minutes and only with the PKCE verifier. |
| `Demos.Local` | Everything in one process, over facilities in memory. |
| `Demos.Ui` | The module for the UI shell: an address for each demo's first page, and nothing else. Every other query has the shell's default address. |
| `Demos.Tests` | The features, over memory and over DynamoDB Local, each entrypoint, and the local host. |

Each project keeps its Terraform in `Infrastructure/`, and the root `Infrastructure/` wires them together. `package.sh` builds the package an environment uses, and CI publishes it as `demos/<version>`.

`demos.json` describes each demo: its title and chapter, what it is, the story its seed data tells, what to try, who a visitor can be in it, and the link that resets it. The sign-in offers its personas, the local host's `services.json` carries it, and the environment passes it to the shell, whose demo panel shows it above the demo's pages, with a button for each persona the text names and **Reset this demo**. `ResetDemo` forgets the demo's data in the visitor's sandbox, so its story starts again. Any address takes `?as=<persona>`, as a link from the book does.

## Running locally

```
export GITHUB_PACKAGES_TOKEN=$(gh auth token)
(cd Demos.Ui && npm install && npm run build)
(cd ../ui && npm install && npm run build)        # the shell, from a sibling checkout of featurewise-ui
dotnet run --project Demos.Local
```

To run the DynamoDB tests too, start DynamoDB Local (`docker run -d -p 8000:8000 amazon/dynamodb-local`) and set `DYNAMODB_LOCAL=http://localhost:8000` before `dotnet test`; without it they're skipped.

Then open the URL it prints, sign in from the pill as any persona, and follow the links. The box office is at `/demos/box-office/performances`. Locally, events reach their workers as soon as they're committed, and the charity collects every minute.
