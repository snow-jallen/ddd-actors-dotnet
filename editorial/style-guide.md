# Style Guide: *Domain-Driven Design with the Actor Model on .NET*

*Editor-in-chief · 2026-11-04 (revision 2: chapters renumbered; Chapter 19 adds the mobile app) · binding on every writer, reviewer and copy-editor agent*

Read this together with `editorial/outline.md`, which sets what each section covers, and `editorial/sample-architecture.md`, which sets where the code lives and what it may use.

---

## 1. Who we are writing for

**The reader:**

- a working C# developer who has shipped web APIs;
- has heard of DDD but has probably not practiced strategic design;
- has never used actors or event sourcing;
- is curious about C# 15.

**The promise:** by the end of the book they can model a real domain, implement it as a functional core inside an Akka.NET actor shell, test it at every level, observe it in production, serve it to an offline-capable mobile client, and ship and evolve it without breaking history.

They will read the book with the sample repo open. Every claim either points at code they can run or at a primary source they can check.

## 2. Voice

- **Second person, warm, rigorous.** Talk to the reader: "You will notice the compiler refuses…".
  - Use "we" only for work done together in the sample ("we add a projection").
  - Never use "we" to mean the authors.
- **First principles before frameworks.** Explain the problem, then the idea, then the Akka.NET or C# mechanism, then the code. A reader should understand *why* a piece of code exists before seeing it.
- **Concrete over abstract.** Every pattern is introduced through Ohmline: a charge point, a site at the bus depot, a driver's RFID card. Avoid "Foo", "Order" and "Customer" examples unless contrasting with the classic literature.
- **Honest.** State trade-offs and the places where the actor mapping is awkward (uniqueness, hot aggregates, queries). When something is a beta, a workaround, or a judgment call, say so.
- **Calm and precise.**
  - No hype ("blazing", "magic", "simply", "just", "easy", "powerful", "seamless").
  - No exclamation marks.
  - No rhetorical questions as section openers.
- **Short paragraphs** (2–5 sentences) and active voice, with one idea per paragraph.
- **Define a term at first use** and add it to the glossary (§8) if it is part of the ubiquitous language.
- **Respect the reader's time.** Do not recap the previous paragraph, do not announce what you are about to say, and do not pad.

## 3. Chapter anatomy

Each chapter is one file: `book/chNN-<slug>.md`. NN and the slug come from `outline.json`; for example, `book/ch07-aggregates-and-the-decider.md`. Front matter goes in `book/00-front-matter.md` and the appendices in `book/appA-<slug>.md` … `book/appE-<slug>.md`.

Every chapter has these parts, in this order:

1. **`# Chapter N. <Title>`**, exactly as in `outline.json`.
2. **Opening hook** (150–300 words). A concrete Ohmline moment that creates the need for the chapter: a depot overload at 07:40, a driver charged twice, a replay that crashes after a rename. No headings inside the hook.
3. **`> **In this chapter you will:**`**, a blockquote with 4–6 bullet points that are outcomes a reader can check, not topics.
4. **`> **Starting point:**`**, naming the tag the chapter starts from. That is the last tag of the previous chapter, or `none` for chapter 1. Include the checkout command.
5. **Sections**, one `## N.M <Section title>` per outline section, with titles exactly as in `outline.json`.
   - Sections without a tag still get a heading.
   - Use `###` for sub-headings inside a section. Never go deeper than `####`.
6. **`## Summary`**: 5–8 bullet points. They must restate what was learned, not what was covered.
7. **`## DDD canon covered`**: a one-line list of the canon items this chapter introduces or deepens, using the names from `research/ddd-canon.md` §8.
8. **`## Exercises`**: 4–6 numbered exercises, each labelled with its kind:
   - **Reflect**: a design question with no single answer. Give the reader the trade-off axes.
   - **Extend**: a change to the sample, starting from a named tag, with a hint of where to start and how to know it worked (a test to write).
   - **Break it**: make a deliberate mistake and observe the failure (for example, "remove the `null` arm and…", "persist the union…"). Say what the reader should expect to see.

   Include at least one of each kind per chapter, except in chapter 1. Solutions are not in the book.
9. **`## Further reading`**: 3–8 entries, primary sources first, as `Author, *Title* (year), URL`, with one line on why to read it.

**Section length:** roughly the chapter's word target divided by its number of sections. Code blocks count toward the word target; prose should still be at least 60% of every chapter.

## 4. Section anatomy and "Try it" boxes

A section that has a tag follows this order:

1. **Motivation:** what problem in Ohmline this solves.
2. **The idea:** the DDD, actor or language concept, from first principles.
3. **The code:** quoted from the sample at this section's tag (§6), with a walkthrough. Show the important part, not every file.
4. **Consequences:** what changed, what it costs, and what to watch out for.
5. **The "Try it" box.** This is always the last element of the section:

```markdown
> **Try it**
>
> ```bash
> git checkout ch07-s04
> git diff ch07-s03 ch07-s04 --stat
> git diff ch07-s03 ch07-s04 -- src/Charging/Ohmline.Charging.Domain
> dotnet test --solution Ohmline.slnx
> ```
>
> You should see 14 new tests in `Ohmline.Charging.Domain.Tests` pass.
```

**Rules for "Try it" boxes:**

- **Diff baseline.** The diff starts from the **previous tag that exists**. Tags are skipped for sections that have no code. Look it up in `outline.json`; never invent a tag.
- **Real observable results.** The closing sentence states something the reader can actually observe, such as a test count, a log line or an HTTP response. Take it from a real run at that tag, recorded in the coder's handoff notes. Never guess it.
- **Prefer `dotnet test`.** Use `dotnet run` and `curl` commands only when the section is about runtime behaviour. Give the exact command and the real output, trimmed with `...`.
- **Side branches** use the same box with `git checkout wrong/ch10-s02-union-on-the-wire` and say what fails.

Sections without code still end with a short practical takeaway, but they have no "Try it" box.

## 5. Markdown conventions

- **Format:** CommonMark plus GitHub tables, fenced code and Mermaid. No raw HTML, no images (use Mermaid), and no footnotes (use Further reading or inline links).
- **Inline code** for every identifier, type, file path, package, command, config key and tag: `ChargePointActor`, `Akka.Persistence.Sql.Hosting`, `ch07-s04`.
- **Fences:** always use a language on a code fence, one of `csharp`, `xml`, `xaml` (MAUI pages, Chapter 19 only), `json`, `bash`, `hocon`, `yaml`, `sql`, `diff`, `text` or `mermaid`.
- **First line of a code block from the sample:** a comment naming the file path relative to `sample/` and the tag:
  - `// File: src/Charging/Ohmline.Charging.Domain/ChargePoints/ChargePoint.cs @ ch07-s04`
  - `<!-- File: Directory.Packages.props @ ch10-s07 -->` for XML and XAML (`<!-- File: mobile/Ohmline.Mobile/Pages/StartChargingPage.xaml @ ch19-s05 -->`)
  - `# File: deploy/k8s/base/node-deployment.yaml @ ch21-s06` for YAML, bash or HOCON
  - `-- File: ... @ ...` for SQL
  - JSON cannot carry comments, so put the same text in an italic line directly above the fence: `*File: global.json @ ch01-s07*`.
- **Elisions:** write `// ...` on a line of its own (or `# ...`, `<!-- ... -->`) wherever lines are omitted. Never elide inside an expression. Never change the code that remains: no renames, no re-indenting beyond removing a common leading indent, and no "simplifying".
- **Line length:** at most 100 characters in code blocks. If the sample's line is longer, the coder fixes the sample. The writer does not wrap it by hand.
- **Block length:** at most 40 lines per code block. Split longer listings into several blocks with prose between them.
- **Diffs:** use a ```` ```diff ```` block taken from `git diff <prev> <tag> -- <path>`. Its first line is a comment naming the command, for example `# git diff ch08-s05 ch08-s06 -- src/Charging/Ohmline.Charging.Domain/Sessions/SessionState.cs`. Diffs are especially encouraged for refactoring sections such as 8.6, 10.10, 19.2 and 20.7.
- **Callouts** are blockquotes that start with one of these bold labels, and no others:
  - `> **Note:**` for useful context.
  - `> **Tip:**` for a practical shortcut.
  - `> **Warning:**` for something that silently loses data, breaks replay or breaks a cluster. Use it sparingly; it should alarm.
  - `> **Version note:**` for behaviour tied to a specific version (RC3, 1.5.73, 1.6 beta) that may change.
  - `> **Anti-pattern: <name>**` for a named anti-pattern, matching the names in Appendix C, plus the `wrong/` branch if there is one.
  - `> **Sidebar: <title>**` for an optional deeper dive, at most about 400 words.
  - `> **Try it**` (see §4).
- **Mermaid:** use it for context maps, EventStorming timelines, state machines (`stateDiagram-v2`), sequence diagrams of message flows and deployment topology. Keep each diagram to about 15 nodes. Every diagram is introduced by a sentence and followed by an explanation of it.
  - Chapters 10, 11, 13 and 14 promise specific sequence diagrams (the persist cycle, a write in flight, recovery, failure, the CQRS pipeline, read-your-writes); each must match what the sample's tests demonstrate, including message order.
  - Use participant names from the code (`ChargePointActor`, `Journal`, `SqlReadJournal`, `SessionHistoryProjection`, `OutboxDispatcher`), not generic boxes.
- **Tables:** use them for comparisons and mappings. A table needs a header row and must still read correctly on a narrow screen. Keep cells short.
- **Cross-references:** write "section 10.3" (lower case) or "Chapter 10". Do not use Markdown anchors.
- **Lists:** use them for parallel items. Explanations go in prose.
- **Numbers and units:** a space between number and unit (22 kW, 7.4 kWh, 32 A, 35 s), "per cent" as `%`, and ISO dates (2026-11-04).

## 6. The code-quoting rule (non-negotiable)

1. **Every line of C#, XML, JSON, YAML, HOCON, SQL or shell configuration presented as part of Ohmline is copied verbatim from the sample repo at the tag named in its `// File:` line.** Writers never write code into the book. If the code you need does not exist in the sample, or is not shaped well enough to quote, file a request to the coder for that section. Do not improvise.
2. **How to obtain code:** `git -C /home/jonathan/code/ddd-actors-dotnet/sample show <tag>:<path>`. Use a script to extract the line range, not memory.
3. **Allowed exceptions**, each clearly labelled:
   - **Shell commands** the reader types (`git`, `dotnet`, `podman`, `kubectl`, `curl`).
   - **Real output** captured from running the sample at that tag, in a `text` block whose first line is `$ <command>`. Trim it with `...`.
   - **Illustrations** that are deliberately not Ohmline, for example the classic Evans cargo example or a two-line syntax reminder. Their first line is `// Illustration (not in the sample)`, and there are at most three per chapter.
   - **Compile-error demonstrations.** These must exist in the repo inside `#if DEMO_COMPILE_ERRORS` (in `learn/` or in a test). Quote the real compiler message from an actual build with the symbol defined.
   - **Third-party configuration** quoted from official documentation, with a link.
4. **Book and repo must agree.** The editor's tooling extracts every fenced block that has a `File:` header and checks that, after removing elision markers, its lines appear in order in `git show <tag>:<path>`. A mismatch fails review.
5. **Never quote code from `research/`.** The research spikes use other names (`Order`, `Cart`, `Shop`) and are not the sample.
6. **When the sample changes after a chapter is written** (a rebase that fixes an earlier section), the editor tells affected writers, who re-extract their blocks.

## 7. Accuracy, versions and sources

- **Versions** are stated exactly as in `sample-architecture.md` §2.4: Akka.NET 1.5.73, .NET SDK 11.0.100-rc.3, C# 15, OpenTelemetry 1.20.1, Aspire 13.6.4, xUnit v3 4.0.2, and so on.
  - Write "at the time of writing (November 2026)" once per chapter at most.
  - Put anything that depends on the RC or a beta in a `> **Version note:**`.
- **No unverified claims.** Research notes mark claims `VERIFIED` or `UNVERIFIED`. An UNVERIFIED claim may appear in the book only after the coder or tester has verified it in the sample, or if it is explicitly hedged ("the documentation states…, which we have not tested") with a link.
  - Behaviour descriptions (defaults, exceptions, log levels) must match what the sample's tests demonstrate.
- **The mobile app (Chapter 19):** a build of the MAUI head proves only that it builds. Any claim that the app behaves a certain way on Android must come from a run on the emulator recorded in the coder's handoff notes; claims about iOS, Mac Catalyst or physical devices are hedged. The Akka.NET trimming and NativeAOT evidence in 19.10 was measured on linux-x64 (`research/mobile.md` §2.2); say so.
- **Durable State:** say plainly that Akka.NET 1.5.73 has no Durable State store, and present 11.7's state-sourced listing as the closest equivalent, not as an Akka.NET feature.
- **Primary sources first.** Cite these before secondary sources:
  - Evans (2003) and the *DDD Reference* (2015), Vernon (2013 and *Reactive Messaging Patterns with the Actor Model*, 2015), Khononov (2021), Wlaschin (2018), Brandolini (EventStorming), Hofer and Schwentner (Domain Storytelling), the ddd-crew canvases, Helland (2007/2016), Hewitt (1973);
  - getakka.net, github.com/akkadotnet, petabridge.com, learn.microsoft.com, github.com/dotnet/csharplang, opentelemetry.io and aspire.dev.

  Each chapter's Further reading lists them with URLs.
- **OCPP and OpenADR:** say "a simplified subset in the style of OCPP 1.6J" or "OpenADR-like". Never claim conformance, and do not reproduce spec text.
- **All companies are fictional:** Ohmline, TallyPay, Northgate Grid and AssetTrack. Do not name real charge point operators, payment providers or grid operators as if they were part of the example.

## 8. Terminology

### 8.1 Spelling and capitalization

| Use | Not |
|---|---|
| Akka.NET (prose), `Akka` (package) | Akka.Net, AKKA |
| C# 15, .NET 11 | C#15, .Net 11, dotnet 11 (except the CLI command `dotnet`) |
| Domain-Driven Design (title), DDD (after first use) | domain driven design |
| event sourcing, CQRS, process manager, saga | Event Sourcing (mid-sentence) |
| bounded context, aggregate, value object, entity (lower case in prose) | Bounded Context mid-sentence, except in pattern names in tables |
| charge point (prose), `ChargePoint` (code) | chargepoint, charging point |
| kWh, kW, A, Wh | KWH, kwh, Kw |
| EV, OCPP, RFID, TOU (time-of-use, defined at first use) | |
| .NET MAUI (prose), "the MAUI head", "the driver app" | Maui, .NET Maui, Xamarin (except when citing history) |
| SignalR, Server-Sent Events (SSE after first use) | Signal-R, server sent events |
| Durable State (the JVM Akka feature), state sourcing (11.7) | "durable state" for Akka.NET as if it shipped one |
| "actor" (Akka) vs "story actor" (Domain Storytelling) | unqualified "actor" in chapter 4 |
| tag `ch07-s04`, branch `wrong/ch10-s02-…` | "commit ch07-s04" |

### 8.2 Ohmline ubiquitous language (the glossary)

These terms come from `docs/glossary.md` in the sample. Use them exactly. When the sample's glossary changes (ch04-s05, ch19-s01, ch20-s07), update this table.

| Term | Context | Meaning | In code |
|---|---|---|---|
| Ohmline | — | The fictional charge point operator (CPO) whose software we build | `Ohmline.*` |
| Site | Site Energy, Charging | A location with one grid connection and one power budget | `SiteId` (kebab string, e.g. `slc-depot`) |
| Grid connection limit | Site Energy | Maximum power the site may draw from the grid | `SiteConfiguration.GridLimit` (`Power`) |
| Safety margin | Site Energy | Headroom kept below the grid limit | `Power` |
| Curtailment | Site Energy | A temporary reduction of the site's capacity requested by the grid operator | `CurtailmentApplied` |
| Effective capacity | Site Energy | Grid limit − active curtailment − safety margin | computed in `SitePowerBudget` |
| Power allocation | Site Energy | Power granted to one connector's session; between the connector's min and max | `PowerAllocation` |
| Priority class | Site Energy | Fleet or Public; fleet requests are served first | `PriorityClass` |
| Fair-share allocation | Site Energy | Water-filling distribution of effective capacity across requests | `FairShareAllocator` |
| Rebalance (allocation) | Site Energy | Recomputing all allocations after a change (not to be confused with *shard rebalancing*) | `AllocationsRebalanced` |
| Charge point (charger) | Charging, Gateway | A physical charging device with one or more connectors | `ChargePointId` (e.g. `OHM-SLC-0042`) |
| Connector | Charging | A numbered plug on a charge point; hosts at most one session | `ConnectorId` (`OHM-SLC-0042/1`) |
| Connector status | Charging | Available, Reserved, Occupied, Faulted (renamed *Out of order* in 20.7), Unavailable | `ConnectorState` union |
| Reservation | Charging | A hold on a connector for one token until it expires | `ConnectorReserved`, `ReservationExpired` |
| Id token (token) | Driver Accounts, Charging | An RFID card or app credential that authorizes charging | `TokenUid` |
| Driver | all | The person charging; never stored by name in events | `DriverRef` (opaque) |
| Charging session (session) | Charging | One charge on one connector from start to end. OCPP calls this a "transaction"; never use "transaction" for it outside the gateway | `SessionId`, `ChargingSession` |
| Meter reading / register | Charging | The charger's cumulative energy counter (Wh) with a timestamp; must never decrease | `MeterReading` |
| Energy delivered | Charging | Energy (kWh) between two readings or over a session | `Energy` |
| Power / current | all | Instantaneous rate (kW) / electrical current (A); never confuse with energy | `Power`, `Current` |
| Suspended by EV / by charger | Charging | Charging paused by the car (full or idle) versus by Ohmline/the charger (curtailment); decides idle fees (8.6) | `SuspendedByEv`, `SuspendedByCharger` |
| Spend cap | Charging | Maximum amount the driver pre-authorized; reaching it stops the session | `SpendCapReached` |
| Tariff | Tariffs | Versioned pricing rules: energy rate, time rate, session fee, idle fee, TOU bands, price cap | `Tariff`, `TariffComponent` union |
| Tariff snapshot | Tariffs, Charging | The tariff version copied into the session at start; later tariff changes never affect it | `TariffSnapshot` |
| Time-of-use (TOU) band | Tariffs | A rate that applies in a window of site-local time | `TimeOfUseBand` |
| Idle fee / grace period | Tariffs | Per-minute fee after charging ends while the car stays plugged in, after a grace period | `IdleFee` |
| Price breakdown | Tariffs, Billing | Line items and total for a session | `PriceBreakdown` |
| Rounding policy | Tariffs | How amounts are rounded per currency and tariff | `RoundingPolicy` |
| Charging journey | Charging | The process manager from "start charging" to settled payment | `ChargingJourney`, `JourneyId` |
| Hold (pre-authorization) | Billing | Money reserved on the driver's card before charging | `PlaceHold`, `HoldPlaced` |
| Capture / void | Billing | Taking the final amount from a hold / releasing it | `CapturePayment`, `VoidHold` |
| Debt | Billing, Charging | An amount owed after capture failed for good | `DebtRecorded` |
| Receipt / invoice | Billing | The driver-facing record of a settled session | Billing tables |
| Charging profile | Gateway | A power limit sent to a charger (`SetChargingProfile`) | OCPP-style action |
| Remote start / stop | Gateway | Commands Ohmline sends to a charger to start or stop | OCPP-style actions |
| Heartbeat / boot notification | Gateway | Liveness ping / first message after a charger connects | OCPP-style actions |
| Session completed (integration event) | Published Language | The fact other contexts consume after a session is priced | `ohmline.charging.session-completed.v1` |
| Charge point listing | Charging | The public description of a charge point that drivers see (display name, access instructions, opening hours); only the latest revision matters | `ChargePointListing` (state-sourced, 11.7) |
| Version (of an aggregate) | Charging, all | The aggregate's sequence number after a command; returned in `CommandAccepted` and stored with read-model rows | `Version`, `last_sequence_nr` |
| Projection lag | read side | Time between an event entering the journal and a read model reflecting it | `GET /projections/status` |
| Driver app | Driver App | Ohmline's .NET MAUI app for drivers; its own bounded context downstream of Charging | `Ohmline.Mobile`, `Ohmline.Mobile.Core` |
| Pending command | Driver App | A command the driver issued that is stored in the phone's outbox and not yet accepted or rejected by the server | `CommandStatus` (`Queued`, `Sending`) |
| Failed to sync | Driver App | A pending command that could not be delivered within the maximum age (24 h) | `CommandStatus.FailedToSync` |
| Offline | Driver App | The phone believes it has no usable network; a hint that pauses sending, never proof | `ScreenState.Offline`, `IConnectivity` |
| Read-model cache | Driver App | The phone's disposable local copy of server read models, kept newest-by-version | `SqliteReadModelCache` |
| TallyPay | external | Fictional payment provider (pre-authorize, capture, void) | `tools/Ohmline.FakePsp` |
| Northgate Grid | external | Fictional grid operator publishing demand-response signals | `tools/Ohmline.FakeGridOperator` |
| AssetTrack | external | Fictional legacy asset registry; a messy CSV export | `tools/legacy-assets/` |

### 8.3 Words with more than one meaning: always disambiguate

| Word | Meanings | Rule |
|---|---|---|
| session | Charging session (Charging); a payment session (Billing, avoid); an HTTP session (never) | Bare "session" means the charging session |
| transaction | OCPP's name for a charging session; a database transaction; a payment transaction | Say "OCPP transaction", "database transaction" or "payment"; never use a bare "transaction" for a session |
| actor | An Akka actor; a role in a domain story | Say "story actor" in Domain Storytelling contexts |
| rebalance | Shard rebalancing (Akka); allocation rebalancing (Site Energy) | Always qualify |
| event | Domain event; integration event; .NET event; EventStorming sticky | Qualify whenever two meanings are in play |
| repository | DDD repository; git repository | Say "sample repo" for git |
| command | Domain command; CLI command | Clear from context; say "shell command" if ambiguous |
| projection | Read-model projection; LINQ projection | Means a read-model projection |
| outbox | The journal as outbox (16.1); Billing's classic transactional outbox (16.3); the phone's command outbox (19.6) | Always qualify: "the journal-as-outbox", "Billing's outbox", "the app's outbox" |
| mailbox | An Akka actor's mailbox; the phone's single-reader `Channel` (19.6) | Say "the dispatcher's channel" or "the actor shape" for the phone; never imply Akka runs on the phone |
| version | An aggregate's sequence number; a contract or manifest version (`.v2`); an app or service release | Qualify: "aggregate version", "manifest version", "app version" |
| state (persistence) | Aggregate state in memory; a snapshot; JVM Akka's Durable State | Say "Durable State" only for the JVM concept and the 11.7 comparison; Akka.NET 1.5.73 has no Durable State store |

### 8.4 DDD and Akka.NET terms

- Use **Evans's pattern names** for DDD patterns (Anticorruption Layer, Open Host Service, Published Language, and so on). Introduce the abbreviation (ACL, OHS) at first use in each chapter.
- **"Aggregate"** means the consistency boundary (the decider plus its shell). Say "aggregate root" only when contrasting with the OO literature.
- **Decider vocabulary:** "Decide", "Evolve", "fold" and "decision table" as defined in 1.6 and 7.4–7.6.
- **Akka.NET terms** follow the official documentation: shard region, entity, message extractor, passivation, remember-entities, split-brain resolver, coordinated shutdown, journal, snapshot store, read journal, persistence id, manifest, serializer id.

## 9. Writer handoff checklist

Before a chapter goes to review:

- [ ] Every heading and every tag matches `outline.json`.
- [ ] Every code block has a `File: … @ tag` header and passes the extraction check (§6.4).
- [ ] Every "Try it" box uses tags that exist and states an observed result taken from a real run.
- [ ] No UNVERIFIED claim appears without a hedge.
- [ ] Glossary terms are used consistently. Any new ubiquitous-language term has been added to §8.2 and to the sample's `docs/glossary.md`, through the coder.
- [ ] The chapter has Summary, DDD canon covered, Exercises (Reflect, Extend and Break it) and Further reading with URLs.
- [ ] The word count is within ±10% of `wordTarget`.
- [ ] No banned words (§2), no exclamation marks, no raw HTML.
