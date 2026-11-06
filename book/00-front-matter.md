# Domain-Driven Design with the Actor Model on .NET

### Functional core, actor shell with C# 15, .NET 11 and Akka.NET

*First edition · November 2026*

*Verified against the .NET 11 SDK 11.0.100-rc.3, C# 15 and Akka.NET 1.5.73. The companion sample repo, `Ohmline`, ships alongside the manuscript in the `sample/` folder.*

*Every company in this book is fictional: Ohmline, TallyPay, Northgate Grid and AssetTrack. The charger protocol is a simplified subset in the style of [OCPP 1.6J](https://openchargealliance.org/protocols/open-charge-point-protocol/) and makes no claim of conformance to any OCPP version.*

# Preface

Most business software does not fail because the code is slow or the framework is wrong. It fails because two correct pieces of code disagree about a rule that neither of them owns. A site draws more power than its grid connection allows, a driver is charged twice, a replay crashes after someone renamed an event. Domain complexity, consistency and concurrency look like three problems, and this book treats them as one.

Domain-Driven Design (DDD) gives you the tools to find the rules and draw the boundaries around them. The actor model gives each boundary a runtime: one mailbox, one writer, one place where a rule is checked. C# 15 adds the last piece, union types and closed hierarchies, so that a decision table can be exhaustive and the compiler can tell you when it is not. The book's stance is *functional core, actor shell*: the rules are pure functions you can test in microseconds, and Akka.NET wraps them with identity, persistence, distribution and time.

## Who this book is for

You are a working C# developer who has shipped web APIs. You have heard of DDD, perhaps read part of Evans's book, [*Domain-Driven Design: Tackling Complexity in the Heart of Software*](https://www.informit.com/store/domain-driven-design-tackling-complexity-in-the-heart-9780321125217), but you have probably not run an EventStorming workshop or drawn a context map that the build enforces. You have never used actors or event sourcing, and you are curious what C# 15 changes.

No prior DDD or actor experience is assumed. Chapter 2 builds an actor by hand from a `Channel` before it touches Akka.NET, and Chapter 3 pins down every language feature the book relies on with learning tests. If you already know Akka.NET, you can skim Chapter 2; if you already practise strategic design, Chapters 4 and 5 will still show you how to make it executable.

By the end of the book you can model a real domain, implement it as a functional core inside an Akka.NET actor shell, test it at every level, observe it in production, serve it to an offline-capable mobile client, and ship and evolve it without breaking history.

## What you will build: Ohmline

Ohmline is a fictional charge point operator. It runs about 300 charging sites (public car parks, workplaces and one bus depot), each with several charge points from different vendors. Drivers start a charge with an RFID card or the driver app. Ohmline has three obligations: keep every site within its grid connection limit, less any curtailment the grid operator asks for; price every session from the tariff locked when it started; and take payment as a pre-authorization hold that is later captured or voided.

The domain was chosen because its invariants are real and quantitative. A connector hosts at most one session. A meter register never goes backwards. The sum of a site's power allocations never exceeds its effective capacity. Time is everywhere: reservations expire, idle fees start after a grace period, and time-of-use bands follow site-local time through daylight-saving changes. Charge points are long-lived, connected and timer-driven, so they behave like actors before you write a line of code.

Over 21 chapters you build, one tagged commit at a time:

- a pure domain core for charge points, charging sessions, site power budgets, tariffs and id tokens, written as deciders over C# 15 unions and closed records;
- an Akka.NET shell that persists those deciders as event-sourced actors, shards them across a cluster on PostgreSQL 17, and runs a charging-journey process manager with timers;
- read models fed by Persistence Query, live updates over Server-Sent Events and SignalR, an Open Host notification feed and contract-tested integration events;
- anticorruption layers for TallyPay (payments), Northgate Grid (demand response), a legacy AssetTrack CSV export and a charger protocol in the style of OCPP 1.6J, plus a Billing service that deliberately uses no actors;
- distributed tracing through the mailbox, domain metrics with a cardinality budget, health checks and SLOs;
- a .NET MAUI driver app with a durable command outbox and a disposable read-model cache, which works in an underground car park with no signal;
- schema evolution with upcasters and a golden corpus, migrations, container images, Kubernetes manifests, a rolling update, a release with release-please, and an upgrade to Akka.NET 1.6.

At the last section tag on `main` the sample holds more than 2,600 tests, from microsecond decider tests to system tests that start the whole Aspire AppHost.

## How the book is organized

The 21 chapters fall into eight parts. Each part depends on the ones before it, but the chapter openings name their starting tag, so you can begin anywhere with the sample checked out at the right place.

**Part I, First Principles (Chapters 1 to 3).** Chapter 1 shows why complexity, consistency and concurrency are one problem, introduces Ohmline and sets up the workbench. Chapter 2 builds actors from first principles and then ports them to Akka.NET. Chapter 3 teaches the C# 15 modeling toolkit through learning tests, including what happens when a union meets a serializer.

**Part II, Strategic Design (Chapters 4 and 5).** Chapter 4 goes from Domain Storytelling and EventStorming to a vocabulary the compiler checks. Chapter 5 draws bounded contexts, makes the context map executable and enforces a pure core in the build.

**Part III, The Functional Core (Chapters 6 to 9).** Value objects and entities, aggregates written as deciders, domain events, services, policies and supple design, and the unit tests that make up most of the test pyramid. No actor appears in this part.

**Part IV, The Actor Shell (Chapters 10 to 12).** Chapter 10 follows one command from mailbox to journal row. Chapter 11 covers recovery, snapshots, state sourcing as the closest Akka.NET equivalent of Durable State, and failure. Chapter 12 shards the aggregates across a cluster and puts application services at the HTTP edge.

**Part V, CQRS, Processes and Integration (Chapters 13 to 16).** Projections and read models, living with eventual consistency, the charging journey as a process manager, and integration with other contexts and external systems through anticorruption layers, an Open Host Service and outboxes.

**Part VI, Instrumentation and Telemetry (Chapters 17 and 18).** One trace through the mailbox, then metrics, logs, health checks, dashboards and SLOs derived from domain events.

**Part VII, The Mobile Edge (Chapter 19).** The driver app as its own bounded context: what belongs on the phone, what stays on the server, and why Akka.NET is not on the device.

**Part VIII, Operations, Releases and Versioning (Chapters 20 and 21).** Evolving events that live forever, then versioning, releasing and running the cluster.

Every chapter ends with a Summary, the DDD canon it covers, exercises and further reading. The appendices hold the version matrix (A), a glossary (B), the tag index and anti-pattern catalogue (C), a DDD-to-Akka.NET mapping reference (D) and a bibliography (E).

## Following along with the sample repo

The book is written to be read with the sample open. Every section that changes code has an annotated git tag named `chNN-sMM`: `ch07-s04` is the state of the sample at the end of section 7.4. Sections that only explain ideas have no tag, so gaps in the numbering are expected; there is no `ch01-s01`, because section 1.1 has no code.

Start by listing the tags with their section titles, in book order:

```bash
cd sample
git tag -n --sort=version:refname
```

To see the sample as it is at the end of a section, check out its tag. To see what that section changed, diff it against the previous tag that exists:

```bash
git checkout ch05-s03
git diff ch05-s02 ch05-s03 --stat
git diff ch05-s02 ch05-s03 -- build
dotnet test --solution Ohmline.slnx
```

A checked-out tag leaves you on a detached `HEAD`. To experiment from a tag, create a branch of your own first, and return to the latest state with `main`:

```bash
git switch -c my-experiments ch07-s04
git checkout main
```

Each section with a tag ends with a **Try it** box that gives these commands for that section and states what you should observe: a test count, a log line or an HTTP response, taken from a real run at that tag. Each chapter opens with a **Starting point** that names the tag it builds on.

Some lessons are clearer when you watch them fail. Side branches sit next to `main` and are never merged:

| Branch | What it holds |
|---|---|
| `wrong/<tag>-<slug>` | A deliberate mistake made at `<tag>`. It builds; a test whose name starts with `Demonstrates_` shows the failure. |
| `alt/<tag>-<slug>` | A design alternative to the choice the book makes at `<tag>`. |
| `upgrade/akka-1.6` | The platform upgrade to Akka.NET 1.6 (section 21.10, tag `ch21-s10`). |

`git branch --all` lists them. Appendix C maps every tag to its section and every `wrong/` branch to the anti-pattern it demonstrates.

The sample also carries a script that builds and tests every tag in a temporary worktree, leaving your own working tree alone:

```bash
tools/verify-tags.sh             # every section tag
tools/verify-tags.sh 'ch07-*'    # only Chapter 7
```

## Setting up your machine

You need different tools at different points in the book. Install them as the chapters ask for them.

### The .NET 11 SDK (from Chapter 1)

The sample's `global.json` names SDK `11.0.100-rc.3.26522.148`, a go-live release candidate, and rolls forward to any later 11.0 SDK. On Linux or macOS, Microsoft's install script adds that exact version beside the SDKs you already have; on Windows, use `dotnet-install.ps1` or the installer:

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 11.0.100-rc.3.26522.148 --install-dir "$HOME/.dotnet"
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
dotnet --version
```

C# 15 is the default language version for `net11.0`, so no project sets `LangVersion`. The `test` section of `global.json` selects Microsoft.Testing.Platform, which is why `dotnet test` always takes `--solution Ohmline.slnx` or `--project <path>`. Section 1.7 explains both settings.

### Containers (from Chapter 12)

Integration tests start PostgreSQL 17 with Testcontainers, and the Aspire AppHost starts it as a container resource, so you need Docker or Podman. The sample was verified with rootless Podman on Linux, which needs its Docker-compatible socket:

```bash
systemctl --user enable --now podman.socket
export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock
```

Chapter 18 runs a production-shaped telemetry stack with `podman compose` (or `docker compose`) from `deploy/observability/compose.yaml`.

### Aspire (from Chapter 12)

The Aspire CLI 13.6 runs Ohmline's local multi-node cluster, its database, the fake payment provider and the charger simulator, with the Aspire dashboard. Install it with the instructions at https://aspire.dev. The AppHost project itself uses `Aspire.AppHost.Sdk/13.6.4`, which restores like any other package. `dotnet run --project src/Ohmline.AppHost` works too.

### .NET MAUI, an Android SDK and a JDK (Chapter 19 only)

All of the driver app's logic lives in `src/Mobile/Ohmline.Mobile.Core`, a plain `net11.0` library that builds and is tested with the rest of the sample. Only the thin .NET MAUI head, in its own solution `mobile/Ohmline.Mobile.slnx`, needs the `maui-android` workload, an Android SDK and a JDK:

```bash
dotnet workload install maui-android
dotnet build mobile/Ohmline.Mobile/Ohmline.Mobile.csproj -t:InstallAndroidDependencies \
  -f net11.0-android -p:AndroidSdkDirectory=$HOME/android-sdk \
  -p:JavaSdkDirectory=$HOME/jdk -p:AcceptAndroidSDKLicenses=True
export ANDROID_HOME=$HOME/android-sdk JAVA_HOME=$HOME/jdk
dotnet build mobile/Ohmline.Mobile.slnx -warnaserror
```

Android Studio's SDK works as well. iOS and Mac Catalyst build only on a Mac with Xcode, and Windows targets only on Windows. If you skip the mobile toolchain, set `OHMLINE_SKIP_MOBILE=1` before running `tools/verify-tags.sh`; every other build and test is unaffected.

### Kubernetes (Chapter 21)

Section 21.6 renders the manifests with `kubectl kustomize deploy/k8s/overlays/dev`, which needs `kubectl` but no running cluster.

> **Tip:** on a slow or busy machine, set `OHMLINE_TIMEFACTOR=3` before `dotnet test`. The actor tests multiply their timeouts by it, as CI does.

## Conventions used in this book

**Code comes from the sample, verbatim.** Every listing that is part of Ohmline is copied from the sample at the tag named in its first line, and the book's tooling checks that it still matches. C# blocks start with `// File: <path> @ <tag>`, XML and XAML with `<!-- File: ... @ ... -->`, YAML, HOCON and shell with `# File: ... @ ...`, and SQL with `-- File: ... @ ...`. JSON cannot carry comments, so a JSON listing has the same text in an italic line above it. Paths are relative to the sample's root.

A line containing only `// ...` (or `# ...`, `<!-- ... -->`) marks omitted lines; nothing else in a listing is changed. A `diff` block starts with the `git diff` command that produced it. A `text` block whose first line starts with `$` is real output from running the sample at that tag, trimmed with `...`.

Three other kinds of code are labelled as such. A block that starts with `// Illustration (not in the sample)` shows a deliberately non-Ohmline example, such as Evans's cargo shipping domain ([*Domain-Driven Design*](https://www.informit.com/store/domain-driven-design-tackling-complexity-in-the-heart-9780321125217), ch. 7 "Using the Language: An Extended Example") or a two-line syntax reminder. Compiler errors are quoted from real builds of code kept in the repo behind `#if DEMO_COMPILE_ERRORS`. Shell commands you type are not marked.

**Callouts** are blockquotes with a bold label:

| Label | Meaning |
|---|---|
| **Note:** | Useful context |
| **Tip:** | A practical shortcut |
| **Warning:** | Something that silently loses data, breaks replay or breaks a cluster |
| **Version note:** | Behaviour tied to a specific version, such as RC3 or a beta, that may change |
| **Anti-pattern:** | A named mistake from the catalogue in Appendix C, often with a `wrong/` branch |
| **Sidebar:** | An optional deeper dive |
| **Try it** | The commands for a section's tag and what you should observe |

**Exercises** come in three kinds. *Reflect* exercises are design questions with no single answer. *Extend* exercises change the sample from a named tag and tell you which test proves it worked. *Break it* exercises make a deliberate mistake and tell you what failure to expect. Solutions are not in the book.

**Language.** Identifiers, types, packages, paths, configuration keys, commands and tags are set in `code`. Ohmline's ubiquitous language is used exactly as defined in Appendix B, and words with more than one meaning are always qualified: a "session" is a charging session, a "story actor" is a role in a domain story, an "allocation rebalance" is not a shard rebalance. Units have a space before them (22 kW, 7.4 kWh, 32 A) and dates are ISO 8601. Cross-references read "section 10.3" or "Chapter 10".

## Versions used

The book and the sample were verified in November 2026 against these versions. Appendix A lists every package and tool with the tag that first uses it.

| Component | Version |
|---|---|
| .NET SDK | 11.0.100-rc.3.26522.148 (go-live release candidate) |
| C# | 15 (the default for `net11.0`) |
| Akka.NET (all `Akka.*` packages on `main`) | 1.5.73 |
| Akka.NET on branch `upgrade/akka-1.6` | 1.6.0-beta5 |
| Aspire | 13.6.4 (CLI 13.6) |
| OpenTelemetry .NET | 1.20.1 |
| xUnit v3 / Shouldly / CsCheck | 4.0.2 / 4.3.0 / 5.0.1 |
| PostgreSQL | 17 |
| .NET MAUI | 11.0.0-rc.2.26505.5 (the workload the RC3 SDK ships) |

The main line stays on Akka.NET 1.5.73, the current stable release, which is what you are likely to run in production. Section 21.10 performs the upgrade to the 1.6 beta on its own branch, so you can see exactly what changes.

> **Version note:** at the time of writing, .NET 11 general availability (GA) is expected later in November 2026. The sample is pinned to RC3 because it was the newest SDK the whole book could be verified against. When GA ships, the sample re-pins in a new commit on `main`: `global.json`, the RC-versioned packages, the MAUI workload and the container base image move together, and lock files are regenerated. The existing section tags keep their RC pins, so every listing and every Try it box still matches its tag. Because `global.json` rolls forward (`latestFeature`), an old tag selects the newest 11.0 SDK you have installed, which will be GA once you install it; its RC packages stay as they are. Appendix A holds the re-pin checklist.
