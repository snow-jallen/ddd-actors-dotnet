# The DDD Canon, Mapped onto the Actor Model (Akka.NET + C# 15 / .NET 11)

Research notes for the book *Domain-Driven Design with the Actor Model on .NET*.
Author: research agent "ddd-canon". Date: 2026-11-04.

Status legend: **VERIFIED** means compiled and/or ran on the local toolchain on 2026-11-04. **SOURCED** means read in a primary source (URL given). **UNVERIFIED** means from memory or secondary material and must be checked before the text goes to print.

Verified companion code is in `research/ddd-canon-snippets/` (projects `Domain`, `Shell`, `Tests`). Every code block below comes from those files, sometimes trimmed. To rebuild:

```bash
source /home/jonathan/code/ddd-actors-dotnet/tools/env.sh
export MSBuildEnableWorkloadResolver=false   # see "Toolchain gotchas" below
cd research/ddd-canon-snippets/Tests && dotnet test
```

The suite has 8 tests. 6 pass. The 2 tests in `SerializationProbe` fail **on purpose**: they document a serialization gotcha (section 6.2).

---

## 0. Versions and toolchain facts (verified 2026-11-04)

| Item | Version | How verified |
|---|---|---|
| .NET SDK | 11.0.100-rc.3.26522.148 (`net11.0`) | `dotnet --version` |
| C# | 15 (default LangVersion on that SDK): `union` declarations and `closed` classes both compile | VERIFIED by compiling |
| Akka, Akka.Persistence, Akka.Cluster.Sharding, Akka.Streams, Akka.Persistence.Query, Akka.Cluster.Tools, Akka.Discovery.KubernetesApi, Akka.Management | **1.5.73** stable (2026-10-05); **1.6.0-beta5** pre-release (published 2026-11-04) | NuGet flat-container index; `gh release list -R akkadotnet/akka.net` |
| Akka.Hosting, Akka.Cluster.Hosting, Akka.Persistence.Hosting, Akka.Hosting.TestKit | **1.5.73**. Versions are now aligned with core Akka. | NuGet |
| Akka.Persistence.Sql (+ .Hosting) | 1.5.73 | NuGet |
| Akka.TestKit.Xunit | 1.5.73. **This is the xUnit v3 package** (depends on `xunit.v3.extensibility.core >= 3.2.2`). | NuGet catalog |
| Akka.TestKit.Xunit2 | 1.5.73, xUnit v2 | NuGet |
| Akka.Analyzers | 0.3.3. It comes in transitively, and the Akka rules (AK1xxx/AK2xxx) are active. | `project.assets.json` |
| Default serializer in Akka 1.5 | Newtonsoft.Json 13.0.1 (`NewtonSoftJsonSerializer`) | VERIFIED at runtime |
| xunit.v3 | 3.2.2 used. The NuGet index also lists 4.0.2, which **we did not test** with Akka.TestKit.Xunit. | NuGet |
| Microsoft.Extensions.Hosting | 10.0.14 stable. 11.0.0-rc.3 is also available. | NuGet |
| OpenTelemetry | 1.20.1 stable (1.21.0-rc.1 pre-release) | NuGet |

**Akka.NET 1.6 (beta) facts relevant to this topic.** SOURCED from https://github.com/akkadotnet/akka.net/blob/dev/GETTING_STARTED_V1.6.md and the 1.6.0-beta5 release notes:
- Every 1.6 package targets `net10.0` only. It should run on .NET 11 through roll-forward. We did not test 1.6 on .NET 11 (UNVERIFIED).
- Akka.Hosting now ships from the main repo, and its versions are lock-stepped with Akka.
- A new source-generated MessagePack serializer, `Akka.Serialization.V2`, uses `[AkkaSerializable(Manifest="...")]`, `[AkkaField(n)]` and `[AkkaSerializer<IProtocol>(name,id)]`.
- **1.6.0-beta5 (released today) "teaches the source generator to serialize C# 15 union types"** as field types, and infers unions over closed hierarchies and interfaces (PR #8888, #8890).
- Rolling from 1.5 to 1.6 has documented wire-compatibility breaks (`BREAKING_CHANGES_V1.6.md`, "Wire" rows).
- Rolling back to 1.5 cannot read persisted data written by 1.6 unless extra configuration is applied.

That last point is a strong candidate for the Operations / versioning chapter.

**Recommendation for the book:** build the main track on **Akka 1.5.73**, which is stable and what production readers will run. Add a clearly marked "Looking ahead: 1.6" sidebar in the serialization and operations chapters that shows Serialization V2 with unions. Revisit before publication. 1.6 GA is not announced.

**Toolchain gotcha (environment, not book content).** While these notes were being written, a concurrent process changed the workloads in the shared `.dotnet` folder. After that, every build failed with `MSB4242 ... Workload set version 11.0.100-rc.3.26524.4 has missing manifests`. Setting `MSBuildEnableWorkloadResolver=false` bypasses the problem for plain console, classlib and test projects. The editor may want to fix the shared SDK (`dotnet workload repair`) once nobody else is installing.

---

## 1. Sources (primary first)

- Eric Evans, *Domain-Driven Design: Tackling Complexity in the Heart of Software* ("Blue Book"), Addison-Wesley 2003/2004.
- Eric Evans, *DDD Reference: Definitions and Pattern Summaries* (2015, CC-BY 4.0): https://domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf. SOURCED: its TOC marks **Domain Events, Partnership, Big Ball of Mud** as "new term introduced since the 2004 book". Because the Reference is CC-BY, the book can quote pattern definitions with attribution.
- Vaughn Vernon, *Implementing Domain-Driven Design* ("Red Book"), Addison-Wesley 2013. Vernon, *Domain-Driven Design Distilled*, 2016. Vernon, "Effective Aggregate Design" Parts I–III (2011): https://www.dddcommunity.org/library/vernon_2011/
- Vaughn Vernon, *Reactive Messaging Patterns with the Actor Model* (Addison-Wesley 2015). It is the only canonical DDD author's book on actors (Scala/Akka). Its examples cover Process Manager, Message Translator, Idempotent Receiver, Guaranteed Delivery and more. **Cite it heavily.**
- Vlad Khononov, *Learning Domain-Driven Design*, O'Reilly 2021. Useful parts: the subdomain heuristics, integration patterns, and his catalog of business-logic patterns (transaction script, active record, domain model, event-sourced domain model) and architectural patterns (layered, ports and adapters, CQRS). Also his communication patterns: model translation, outbox, saga, process manager.
- Scott Wlaschin, *Domain Modeling Made Functional*, Pragmatic 2018. Key ideas: "make illegal states unrepresentable", workflows as pipelines, choice types as the main modeling tool, errors as values. C# 15 unions finally make this idiomatic in C#.
- Alberto Brandolini, *Introducing EventStorming* (Leanpub) and https://www.eventstorming.com/. Covers Big Picture, Process Level and Design Level sessions. The sticky colors are orange for events, blue for commands, yellow for actors and aggregates, lilac for policies, pink for external systems, and green for read models.
- Stefan Hofer and Henning Schwentner, *Domain Storytelling* (Addison-Wesley 2021); https://domainstorytelling.org/
- ddd-crew (SOURCED):
  - Context Mapping: https://github.com/ddd-crew/context-mapping. It lists 9 patterns plus 3 team relationships: Mutually Dependent, Upstream/Downstream, Free.
  - Bounded Context Canvas **V5**: https://github.com/ddd-crew/bounded-context-canvas
  - Aggregate Design Canvas: https://github.com/ddd-crew/aggregate-design-canvas
- Pat Helland, "Life Beyond Distributed Transactions: An Apostate's Opinion" (CIDR 2007; ACM Queue 2016). It describes "entities" with unique keys that are atomically updatable but not across entities, and "activities" that track partner state. That is almost exactly a sharded persistent actor plus a process manager, and gives the book an excellent first-principles bridge.
- Gary Bernhardt, "Functional Core, Imperative Shell" (Destroy All Software, 2012) and the "Boundaries" talk.
- Jérémie Chassaing, "Functional Event Sourcing Decider" (2021): https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider. It formalizes the decide/evolve pair used in this document.
- C# 15 unions proposal: https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md
- C# 15 closed hierarchies proposal: https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/closed-hierarchies.md
- Akka.NET docs (the site is frozen on 1.5):
  - https://getakka.net/articles/persistence/event-sourcing.html
  - https://getakka.net/articles/clustering/cluster-sharding.html
  - https://getakka.net/articles/actors/reliable-delivery.html
  - https://getakka.net/articles/persistence/persistence-query.html
- Chris Richardson, Transactional Outbox: https://microservices.io/patterns/data/transactional-outbox.html
- Hohpe and Woolf, *Enterprise Integration Patterns*: Process Manager, Message Translator, Idempotent Receiver, Content Enricher.
- Prior art (UNVERIFIED maintenance status; treat as historical): Akkatecture (aggregate-root-as-actor library for Akka.NET, 0.8.0 on NuGet).

---

## 2. The recommended stance: Functional Core, Actor Shell

**We recommend this approach, and it should be the book's spine.**

**The core** is plain C# with no Akka reference. It holds value objects, entities, aggregate state, commands, events and errors, all as records and C# 15 unions. Behaviour lives in two pure functions per aggregate, the *Decider*:

```
Decide : State × Command → Events[] | Error      // business rules / invariants
Evolve : State × Event   → State                 // state transitions; total, never fails
```

**The shell** is an Akka.NET `ReceivePersistentActor` that owns everything effectful:
- identity (`PersistenceId`) and placement (sharding)
- concurrency (one message at a time is the aggregate's lock)
- durability (`Persist`/`PersistAll`, snapshots, recovery)
- time (inject `TimeProvider`)
- messaging (reply, `Tell` other actors), timers and supervision

**Why this approach:**

1. **The actor model gives the right *runtime* boundary but no help with *modeling*.** Everything in an actor is mutable, and a `Receive` lambda invites mixing I/O with rules. DDD's tactical patterns concern the model, so keeping the model pure keeps it about the domain.
2. **Testability.** Decide/Evolve are tested as Given-events / When-command / Then-events tables with no ActorSystem, no TestKit, no async and no timing. This makes up the bulk of the test pyramid. TestKit tests then shrink to protocol, persistence and recovery checks. VERIFIED: see `Tests/OrderTests.cs`.
3. **Event sourcing falls out naturally.** Recovery is `events.Aggregate(Initial, Evolve)`, the same fold the tests use.
4. **Portability and versioning.** The persisted shapes (events, snapshots) live in the core and are versioned there. The Akka version can change (1.5 → 1.6) without touching domain logic.
5. **Unions make illegal states unrepresentable** (Wlaschin) in C#. Lifecycle states (`DraftOrder | SubmittedOrder | CancelledOrder`) become types, and the compiler checks exhaustiveness (warning CS8509). VERIFIED.
6. **It counters the most common Akka.NET anti-pattern**: business logic smeared across `Receive<T>` handlers, each mutating fields, with invariants enforced inconsistently.

**Trade-offs to state honestly in the book:**
- There is a bit more ceremony than "just write the actor".
- Allocations rise slightly because state is immutable. This is negligible at aggregate scale.
- Some behaviours are inherently temporal (timeouts, retries, passivation). They belong in the shell but are *driven by* state the core computes. See the process manager in section 5.3.

### 2.1 Verified core (trimmed from `Domain/Order.cs`)

```csharp
// Commands, events, state and errors are all C# 15 unions over sealed records.
public sealed record PlaceOrder(OrderId Id, string CustomerId);
public sealed record AddLine(OrderId Id, OrderLine Line);
public sealed record SubmitOrder(OrderId Id);
public sealed record CancelOrder(OrderId Id, string Reason);
public union OrderCommand(PlaceOrder, AddLine, SubmitOrder, CancelOrder);

public sealed record OrderPlaced(OrderId Id, string CustomerId, DateTimeOffset At);
public sealed record LineAdded(OrderId Id, OrderLine Line);
public sealed record OrderSubmitted(OrderId Id, Money Total, DateTimeOffset At);
public sealed record OrderCancelled(OrderId Id, string Reason);
public union OrderEvent(OrderPlaced, LineAdded, OrderSubmitted, OrderCancelled);

public sealed record NoOrder;
public sealed record DraftOrder(OrderId Id, string CustomerId, ImmutableList<OrderLine> Lines);
public sealed record SubmittedOrder(OrderId Id, Money Total);
public sealed record CancelledOrder(OrderId Id);
public union OrderState(NoOrder, DraftOrder, SubmittedOrder, CancelledOrder);

public union OrderError(DomainError.InvalidValue, DomainError.InvariantViolated, DomainError.WrongState);
public union Decision(OrderEvent[], OrderError);

public static class Order
{
    public const int MaxLines = 50;
    public static readonly OrderState Initial = new NoOrder();

    public static Decision Decide(OrderState state, OrderCommand cmd, TimeProvider clock) =>
        (state, cmd) switch
        {
            (NoOrder, PlaceOrder p) => new OrderEvent[] { new OrderPlaced(p.Id, p.CustomerId, clock.GetUtcNow()) },
            (DraftOrder d, AddLine) when d.Lines.Count >= MaxLines =>
                new OrderError(new DomainError.InvariantViolated($"An order may have at most {MaxLines} lines")),
            (DraftOrder, AddLine a) when a.Line.Quantity <= 0 =>
                new OrderError(new DomainError.InvariantViolated("Quantity must be positive")),
            (DraftOrder, AddLine a) => new OrderEvent[] { new LineAdded(a.Id, a.Line) },
            (DraftOrder d, SubmitOrder) when d.Lines.IsEmpty =>
                new OrderError(new DomainError.InvariantViolated("Cannot submit an empty order")),
            (DraftOrder d, SubmitOrder s) => new OrderEvent[] { new OrderSubmitted(s.Id, Total(d), clock.GetUtcNow()) },
            (DraftOrder or SubmittedOrder, CancelOrder c) => new OrderEvent[] { new OrderCancelled(c.Id, c.Reason) },
            (CancelledOrder, CancelOrder) => Array.Empty<OrderEvent>(),          // idempotent no-op
            var (s, c) => new OrderError(new DomainError.WrongState(
                              s.Value?.GetType().Name ?? "null", c.Value?.GetType().Name ?? "null")),
        };

    public static OrderState Evolve(OrderState state, OrderEvent evt) =>
        (state, evt) switch
        {
            (NoOrder, OrderPlaced p)       => new DraftOrder(p.Id, p.CustomerId, []),
            (DraftOrder d, LineAdded l)    => d with { Lines = d.Lines.Add(l.Line) },
            (DraftOrder, OrderSubmitted s) => new SubmittedOrder(s.Id, s.Total),
            (_, OrderCancelled c)          => new CancelledOrder(c.Id),
            _ => state,
        };
}
```

VERIFIED observations about C# 15 unions (SDK 11.0.100-rc.3):
- **Patterns unwrap automatically.** A pattern applied to a union value matches against its contents (`Value`). That includes positional tuple patterns like `(DraftOrder d, AddLine a)` over `(OrderState, OrderCommand)`.
- **Implicit conversions from case types work everywhere.** The `switch` arms above return `OrderEvent[]` or `OrderError` directly as `Decision`. `new OrderEvent[] { new OrderPlaced(...) }` converts each element.
- **Exhaustiveness is checked.** Dropping an arm produces `warning CS8509 ... the pattern 'C' is not covered`. With every case present, no discard arm is needed.
- **`closed` records compile too** (`public closed record Gate; public sealed record Open : Gate;`), and exhaustive switches over them give no warning. Book guidance:
  - Use `union` when the cases are independent types, especially ones reused in several unions.
  - Use a `closed` hierarchy when the cases share data or behaviour through a base record.
- **A union declaration is a struct holding one `object?` reference.** Value-type cases are boxed on entry (SOURCED: unions.md, "Union declarations"). `default(OrderState)` has `Value == null`, so guard with `?.` or start from an explicit `Initial`.

---

## 3. Strategic design checklist → actor-model mapping

For each item: what it is (source), how it maps onto Akka.NET, and pitfalls.

### 3.1 Domain, subdomains (core / supporting / generic)
- **Canon:**
  - Evans, Distillation: Core Domain and Generic Subdomains.
  - Vernon IDDD ch.2 adds "Supporting".
  - Khononov ch.1 gives the heuristics: competitive advantage, complexity, volatility, and buy vs build.
  - Bounded Context Canvas V5 adds the *Business model role* (revenue / engagement / compliance) and *Evolution* (genesis / custom / product / commodity, from Wardley mapping).
- **Actor mapping:** subdomain type decides **how much Akka you use**.
  - Core: event-sourced persistent actors with full Decider purity, sharded.
  - Supporting: plain actors or even CRUD behind an actor facade. Khononov's "transaction script / active record" fits here.
  - Generic: buy it. Put an ACL actor in front of the SaaS API.
- **Pitfall:** "everything is an event-sourced sharded actor". The actor model is a runtime choice and should be spent where complexity and concurrency justify it.

### 3.2 Ubiquitous Language
- **Canon:** Evans Part I. One language per bounded context, used in code, conversations and tests.
- **Actor mapping:**
  - Message type names *are* the language. Commands are imperative (`SubmitOrder`) and events are past tense (`OrderSubmitted`).
  - Actor names should be domain nouns (`order`, `fulfillment`), never `OrderManagerActor`.
  - Sharding type names (`"order"`) and `PersistenceId` prefixes (`order-{id}`) become **durable vocabulary**: renaming them later is a data migration. Teach "choose persisted names carefully" early.
- **Unions help:** `union OrderCommand(...)` is a single, compiler-checked glossary of what the aggregate can be asked to do.
- **Pitfall:** technical message names such as `UpdateOrderMsg` or `SetStatus` signal CRUD thinking and anemic actors.

### 3.3 Bounded Context
- **Canon:** Evans Ref p.2. An explicit boundary inside which a model applies.
- **Actor mapping:**
  - Typically one deployable service containing one ActorSystem, often a cluster.
  - Inside it, actor types belong to exactly one context.
  - Within a cluster, use **cluster roles** (`ShardOptions.Role`, `ClusterOptions.Roles`) to keep contexts' entities on their own nodes when they share a cluster.
  - Prefer **separate ActorSystems per bounded context**, talking over published contracts (HTTP/gRPC/Kafka or Akka Remote with a published message assembly). VERIFIED that the Akka.Hosting API for this compiles: `WithShardRegion<T>(..., new ShardOptions{ Role="sales", StateStoreMode=DData })`.
- **Pitfall:** one giant cluster where every context `Tell`s every other context's internal actors directly. That is a distributed Big Ball of Mud: a deploy of one context breaks the wire protocol of another.

### 3.4 Context Map and relationship patterns
SOURCED from the ddd-crew context-mapping repo and the Evans Reference. There are 9 patterns plus team relationships (Mutually Dependent, Upstream/Downstream, Free).

| Pattern | Definition (short) | Actor/Akka.NET mapping |
|---|---|---|
| **Partnership*** | Two teams succeed or fail together and coordinate planning | Shared release train; co-versioned message contracts; possibly the same cluster with roles |
| **Shared Kernel** | Explicitly shared subset of model and code | Shared NuGet package of *messages and value objects only* (no actors). Version it strictly. It must be serializer-stable, because it crosses the wire. |
| **Customer/Supplier** | Upstream plans for downstream needs | Upstream publishes versioned integration events; the downstream's needs drive its event schema backlog |
| **Conformist** | Downstream adopts the upstream model as-is | Downstream actors consume upstream message types directly. Cheap, but couples you to upstream wire versions. |
| **Anticorruption Layer** | Downstream translation layer isolates its model | A **translating actor** (EIP *Message Translator*) at the edge. The translation is a pure function and the actor just routes. VERIFIED example below. |
| **Open Host Service** | Protocol open to all integrators | Akka.Streams/Kafka or gRPC/HTTP endpoint backed by a projection; or a sharding *proxy* (`StartProxy`) for in-cluster consumers |
| **Published Language** | Well-documented shared exchange language | Versioned contract package; schema (protobuf/MessagePack/JSON Schema). The `[AkkaSerializable(Manifest="submit-order-v1")]` manifests in Akka 1.6 V2 are a natural fit (SOURCED). |
| **Separate Ways** | No integration | No messages; maybe duplicated data |
| **Big Ball of Mud*** | Mixed models, unclear boundaries; quarantine it | Wrap it in an ACL actor and never let its types into your messages |

\* = new since the 2004 book (Evans Reference 2015, VERIFIED from the PDF TOC).

The VERIFIED ACL actor from `Shell/Extras.cs`:

```csharp
public sealed record ErpOrderShipped(string ORDER_NO, string SHIP_DT, int STATUS_CD); // upstream shape
public sealed record ShipmentConfirmed(OrderId Order, DateOnly ShippedOn);           // our language

public sealed class ErpAclActor : ReceiveActor
{
    public ErpAclActor(IActorRef downstream)
    {
        Receive<ErpOrderShipped>(m =>
        {
            if (Translate(m) is { } translated) downstream.Tell(translated);
            else Context.System.EventStream.Publish(new Akka.Event.UnhandledMessage(m, Sender, Self));
        });
    }
    public static ShipmentConfirmed? Translate(ErpOrderShipped m) =>          // pure, unit-testable
        m.STATUS_CD == 40 && Guid.TryParseExact(m.ORDER_NO, "N", out var g)
                          && DateOnly.TryParseExact(m.SHIP_DT, "yyyyMMdd", out var d)
            ? new ShipmentConfirmed(new OrderId(g), d) : null;
}
```

**Book idea:** make the context map *executable*. Each relationship type on the map corresponds to a concrete artifact in the sample repo: a contract package, an ACL actor, or a proxy.

### 3.5 Distillation (Evans Ref Part V)
- **The patterns:** Core Domain, Generic Subdomains, Domain Vision Statement, Highlighted Core, Cohesive Mechanisms, Segregated Core, Abstract Core.
- **Actor mapping:**
  - *Cohesive Mechanisms* go in separate libraries or actors: a pricing engine, or a rules evaluator called synchronously inside Decide as a pure function.
  - *Segregated Core* means the Domain project has zero Akka references. Enforce it with an architecture test (NetArchTest, ArchUnitNET: UNVERIFIED current versions) or simply by the project graph.
  - *Abstract Core* means interfaces and unions shared across modules.
- **Book use:** one section showing the solution layout as distillation made physical.

### 3.6 Large-scale structure (Evans Ref Part VI)
- **The patterns:** Evolving Order, System Metaphor, Responsibility Layers, Knowledge Level, Pluggable Component Framework.
- **Actor mapping:**
  - *Responsibility Layers* map loosely onto the **actor hierarchy**: guardians and supervisors (operations), entities (decisions), projections (analysis).
  - *Knowledge Level* (configurable rules as data) can be a cluster singleton or DistributedData holding policy configuration that the core reads as a parameter.
  - *Evolving Order* is the honest stance: the hierarchy will change, so keep the supervision structure shallow.
- **Coverage:** briefly. This part is the least-practiced in modern DDD (Khononov omits most of it). Mention for completeness.

### 3.7 Collaborative modeling techniques (modern practice)
- **EventStorming** (Brandolini):
  - *Big Picture* finds contexts and pivotal events.
  - *Process Level* surfaces policies, which are lilac "whenever X then Y" stickies; these become **process manager actors or event subscribers**.
  - *Design Level* yields aggregates (yellow), commands (blue) and events (orange).
  - **Mapping:** orange → `OrderEvent` union cases; blue → `OrderCommand` cases; yellow aggregate → persistent entity actor; lilac policy → process manager / saga actor; green read model → Persistence Query projection; pink external system → ACL actor.
  - This mapping is one-to-one and should be a table plus a figure in the book.
- **Domain Storytelling** (Hofer/Schwentner): actors, work objects and activities in numbered sentences. Good for finding the ubiquitous language and context boundaries. Its "actor" means a human or system role, *not* an Akka actor. **Warn readers about the terminology collision.**
- **Bounded Context Canvas V5** (SOURCED): Name, Purpose, Strategic Classification, Domain Roles, Inbound Communication, Outbound Communication, Ubiquitous Language, Business Decisions, Assumptions, Verification Metrics, Open Questions.
  - **Mapping:** inbound and outbound messages (commands / queries / events) become the context's **public message contracts**. That is exactly the set of types that must be serializer-stable and versioned.
- **Aggregate Design Canvas** (SOURCED): Name, Description, State Transitions, Enforced Invariants, Corrective Policies, Handled Commands, Created Events, **Throughput**, **Size**.
  - **This canvas maps extraordinarily well to actors.** State Transitions become the `OrderState` union. Invariants become the `when` guards in Decide. Corrective Policies become process managers. Commands and events become the unions.
  - **Throughput** is the actor's mailbox rate: one actor processes one message at a time, so a hot aggregate is a hot mailbox.
  - **Size** is the event count: it drives the snapshot interval, recovery time, and "scope the aggregate to a period" (e.g. a billing cycle) so persistence IDs don't grow without bound.

---

## 4. Tactical design checklist → actor-model mapping

### 4.1 Entities
- **Canon:** identity and continuity over time (Evans Ref p.11).
- **Mapping:**
  - The *aggregate-root* entity becomes a persistent actor whose identity is the shard entity ID plus `PersistenceId`.
  - *Inner* entities (e.g. an `OrderLine` with its own identity) are **records inside the aggregate state, not actors.**
  - Strongly typed IDs as `readonly record struct OrderId(Guid Value)` with `Guid.CreateVersion7()`, which gives time-ordered IDs and is .NET 9+. VERIFIED, compiles.
- **Anti-pattern: actor-per-row / actor-per-entity-everywhere.** Turning every entity (each order line, each address) into an actor creates chatty asks and distributed invariants. It also makes consistency impossible: you can't atomically check "≤ 50 lines" across 50 actors.

### 4.2 Value Objects
- **Canon:** immutable, defined by attributes, side-effect-free behaviour (Evans Ref p.12; Vernon IDDD ch.6).
- **Mapping:**
  - Value objects map perfectly onto `sealed record` / `readonly record struct`.
  - Akka *requires* immutable messages, so VOs double as safe message payloads.
  - Use smart constructors that return a union: `Result<Money> Create(...)` where `union Result<T>(T, DomainError.InvalidValue)`. VERIFIED, generic unions compile.
- **GOTCHA (VERIFIED):** a VO with only a private constructor (the usual "always-valid" pattern) **fails to deserialize** with Akka 1.5's default Newtonsoft serializer: `JsonSerializationException: Unable to find a constructor to use for type Sales.Domain.Money`. This bites as soon as events hit a real journal or cross the network. The in-memory journal in tests does not serialize, so tests pass and production fails. Fixes, in the book's order of preference:
  1. A dedicated serializer with explicit DTOs, mapped in the shell.
  2. Akka 1.6 Serialization V2 with `[AkkaField]`, once GA.
  3. `[JsonConstructor]` on the private constructor. This leaks infrastructure into the core, so it is the least preferred.

  Always turn on `akka.actor.serialize-messages = on` in tests (see 6.2).

### 4.3 Aggregates and Vernon's four rules
- **Canon:** a consistency boundary with a root; external references go to the root only (Evans Ref p.16). Vernon, "Effective Aggregate Design" (2011) and IDDD ch.10:
  1. **Model true invariants in consistency boundaries.**
  2. **Design small aggregates.**
  3. **Reference other aggregates by identity.**
  4. **Use eventual consistency outside the boundary.**
- **Mapping:** this is the strongest correspondence in the whole book.

  | Vernon rule | Actor-model realization |
  |---|---|
  | 1. Invariants inside the boundary | One persistent actor = one consistency boundary. Its sequential mailbox is the lock, so no optimistic-concurrency code is needed inside one node. Sharding guarantees *a single active instance cluster-wide* (except in split-brain without a downing provider; cover SBR in ops). |
  | 2. Small aggregates | Small state → fast recovery, small snapshots, lower mailbox contention (canvas *Throughput*/*Size*). |
  | 3. Reference by identity | Messages and state carry `OrderId`, **never `IActorRef`** for other aggregates. Sharding resolves IDs to locations. An `IActorRef` is an address that dies on rebalance; an ID is stable. |
  | 4. Eventual consistency outside | Cross-aggregate rules use events plus a process manager (or a projection-fed policy), never a two-actor transaction. Helland's "activities" give the theory. |

- **Invariants:** all checks live in `Decide` as guards. `Evolve` never validates, because events are facts and replay must not fail (SOURCED: the Akka event-sourcing page says the same).
- **Anti-patterns:**
  - *Anemic actor:* `Receive<SetStatus>(m => _status = m.Status)` with rules in a calling service.
  - *Fat actor:* an aggregate actor that also performs HTTP calls, sends emails, and queries other actors with `Ask` mid-decision.
  - *Cross-aggregate ask chains:* `await orders.Ask(...)` inside `inventory`'s handler. This creates deadlock and timeout risk, holds the mailbox, and breaks rule 4.

### 4.4 Domain Events
- **Canon:** new in the Evans 2015 Reference. Something that happened that domain experts care about. Vernon IDDD ch.8.
- **Mapping:**
  - Events are what the persistent actor `Persist`s. With event sourcing, domain events and persistence events are the same objects, which is the beauty of the combination.
  - Publish internally via the `EventStream` (local only) or **Persistence Query `EventsByTag`** (durable, cross-node).
  - Tag with an `IWriteEventAdapter` returning `Tagged`. VERIFIED, compiles via Akka.Persistence.Hosting:

```csharp
public sealed class OrderTaggingAdapter : IWriteEventAdapter
{
    public string Manifest(object evt) => string.Empty;
    public object ToJournal(object evt) => evt switch
    {
        OrderSubmitted or OrderCancelled => new Tagged(evt, ImmutableHashSet.Create("order", "order-lifecycle")),
        OrderPlaced or LineAdded         => new Tagged(evt, ImmutableHashSet.Create("order")),
        _ => evt,
    };
}
// registration
b.WithInMemoryJournal(adapters => adapters.AddWriteEventAdapter<OrderTaggingAdapter>("order-tagger",
    [typeof(OrderPlaced), typeof(LineAdded), typeof(OrderSubmitted), typeof(OrderCancelled)]));
```

- **Gotcha (VERIFIED):** persist the **case-type record** (`e.Value!`), never the union struct. Reasons:
  - The union wrapper boxes into an `OrderEvent` object.
  - Default Newtonsoft deserialization of a union silently loses its contents (section 6.2).
  - Akka's `Recover<OrderPlaced>` dispatch is by runtime type.

### 4.5 Domain events vs integration events
- **Canon:** Vernon IDDD ch.13; Khononov ch.9 (model translation, outbox).
- **Mapping:**
  - **Domain events** are private to the context. They are journal records and can evolve with internal schema migration (event adapters, upcasting).
  - **Integration events** are part of the Published Language. They are produced by a **projection** that reads `EventsByTag` and translates to a public, versioned contract, then publishes it (Kafka, or Akka.Streams to a broker).
- **This split is the most important versioning boundary in the book's Operations chapter.**
- **Pitfall:** publishing journal events directly to other contexts. That freezes your internal model.

### 4.6 Factories
- **Canon:** encapsulate complex creation (Evans Ref p.18).
- **Mapping:**
  - The *domain* factory is a static creation function or the `(NoOrder, PlaceOrder)` arm of Decide. The aggregate's "birth" is just its first event.
  - The *actor* factory is `Props` (`OrderActor.PropsFor(id, clock)`) plus the sharding entity-props factory, which is where DI comes in (`(_, _, resolver) => id => ...` in Akka.Hosting, VERIFIED). Keep these two distinct in the text.

### 4.7 Repositories
- **Canon:** collection-like access to aggregates by identity (Evans Ref p.17).
- **Mapping:**
  - The **shard region is the repository**: `region.Tell(new OrderEnvelope(id, cmd))` is "get by ID and invoke".
  - The `IMessageExtractor` (e.g. `HashCodeMessageExtractor`) is the identity map. VERIFIED:

    ```csharp
    public sealed class OrderMessageExtractor() : HashCodeMessageExtractor(maxNumberOfShards: 100)
    {
        public override string? EntityId(object message) => message switch
        { OrderEnvelope e => e.OrderId, GetOrderState q => q.OrderId, _ => null };
    }
    ```

  - Persistence (the journal) is the storage behind it. **Loading** is recovery; **saving** is `Persist`. There is no `Save(aggregate)` call.
  - Optional application-layer facade: an `IOrderRepository`-like *interface* in the application layer that wraps `IActorRef` region asks. Use it for HTTP endpoints.
- **Sharding facts (SOURCED, cluster-sharding page):**
  - `maxNumberOfShards` must never change for the life of the cluster; aim for about 10× the max node count.
  - `state-store-mode = ddata` is recommended. `persistence` mode is "being deprecated after 1.5".
  - Automatic passivation: `passivate-idle-entity-after`. It is disabled when remember-entities is on.
  - Do NOT `Context.Stop(Self)` in sharded entities; send `ShardRegion.Passivate` to the parent.
  - Since 1.5.15, `StartEntity` is handled automatically, and handling it in the extractor triggers analyzer AK2001.
- **Pitfall:** querying across many aggregates via a "repository FindAll" that asks every entity. That is a read model's job (CQRS).

### 4.8 Domain Services
- **Canon:** stateless operations that don't belong to an entity or VO (Evans Ref p.14).
- **Mapping:**
  - Usually just **pure functions** passed into or called from Decide (pricing, tax calculation).
  - If a service needs I/O (e.g. a credit check), it is *not* a domain service in the core. It becomes an actor or adapter in the shell, and its *result* enters the aggregate as a command or event: "decide with data you were given".
  - **Do not inject `IHttpClientFactory` into an aggregate actor.**
- **Pitfall:** "service actors" that become god objects which every aggregate asks synchronously.

### 4.9 Application Services
- **Canon:** thin orchestration of use cases, transactions and security (Vernon IDDD ch.14).
- **Mapping:**
  - ASP.NET Core minimal API endpoints or gRPC handlers that validate DTOs, map them to commands, and do `await region.Ask<...>(envelope, timeout)` at the **edge only**.
  - Within the cluster, prefer `Tell` with reply-to. `Ask` is acceptable at the HTTP boundary because a request/response is inherent there.
  - **Idempotency key** on every external command (the HTTP header becomes a command field). The aggregate dedups via state, as in the VERIFIED `(CancelledOrder, CancelOrder) => []` arm, or via a bounded set of processed IDs in state.

### 4.10 Modules
- **Canon:** Evans Ref p.15. Packaging by domain concept.
- **Mapping:**
  - .NET projects and namespaces per bounded context: `Sales.Domain` (pure), `Sales.Shell` / `Sales.Actors`, `Sales.Contracts` (published language), `Sales.Api`, `Sales.Projections`.
  - Use `internal` plus `closed` hierarchies so case sets can't be extended outside the module (closed hierarchies are same-assembly only, SOURCED).

### 4.11 Specifications
- **Canon:** Blue Book ch.9 "Making Implicit Concepts Explicit". Note it is *not* in the 2015 Reference TOC.
- **Mapping:**
  - Pure predicates in the core. VERIFIED: `sealed record LargeOrderSpec(decimal Threshold) : ISpecification<SubmittedOrder>`.
  - Uses: validation inside Decide, selection in projections, and process-manager routing.
  - In C# 15, many specifications are simply **list/property patterns** (`o is { Total.Amount: >= 1000m }`). Show both forms.

### 4.12 Supple design (Evans Ref Part III)
- **Intention-Revealing Interfaces:** command and event names, and `Decide`/`Evolve` signatures that read like the language.
- **Side-Effect-Free Functions:** the entire functional core. `TimeProvider` is passed in (testable with `FakeTimeProvider`, from the Microsoft.Extensions.TimeProvider.Testing package, VERIFIED) rather than calling `DateTime.UtcNow`.
- **Assertions:** invariants as Decide guards, plus property-based tests of Evolve (FsCheck / CsCheck: UNVERIFIED current versions).
- **Standalone Classes:** VOs with no dependencies.
- **Closure of Operations:** `Money.Add(Money) → Money`. VERIFIED.
- **Declarative Design:** the `switch` over `(state, command)` tuples *is* a declarative decision table.
- **Conceptual Contours:** union case boundaries follow domain contours.
- **Drawing on Established Formalisms:** finite state machines. Akka's `Become` is a runtime FSM. We recommend the **union state + pure transition function** form instead of `Become`, which is untestable without actors. Show `Become` once as a contrast.

### 4.13 Model-Driven Design, Hands-on Modelers, Refactoring Toward Deeper Insight, Continuous Integration (Evans Ref Part I)
- Cover these in the "first principles" chapters.
- **Actor-specific angle:** refactoring an event-sourced model means **events are forever**. Deeper insight usually means new event types plus upcasters, not editing old ones. This connects directly to the versioning chapter.

---

## 5. Architectural patterns checklist → actor-model mapping

### 5.1 Layered / Hexagonal (Ports and Adapters) / Clean / Onion
- **Canon:** Evans Layered Architecture; Cockburn's hexagonal; Vernon IDDD ch.4; Khononov ch.8.
- **Mapping:**
  - Domain (pure core) ← Application (commands, use-case handlers) ← Infrastructure/Adapters (actors, persistence plugins, HTTP, Kafka).
  - **Actors are adapters/infrastructure, not domain.** Persistence plugins (Akka.Persistence.Sql) are driven adapters. HTTP endpoints are driving adapters.
  - Dependency rule: `Domain.csproj` references nothing Akka. VERIFIED by the snippet project graph.

### 5.2 CQRS
- **Mapping:**
  - The write side is the sharded persistent actors.
  - The read side is **Persistence Query** projections: `ReadJournalFor<SqlReadJournal>(...).EventsByTag("order", offset)` → Akka.Streams → read-model DB.
  - Projections must store their own offset to resume, and the pattern is not built in (SOURCED). Run projections as a **cluster singleton** or a sharded daemon process (`ShardedDaemonProcess` in Akka.Cluster.Sharding; UNVERIFIED that it is still in 1.5.73, though it was introduced in 1.4.x).
- **Pitfall:** answering queries by asking aggregates (`GetOrderState` to each actor) for lists or dashboards. Querying one aggregate's own state for a command-side UI is fine.

### 5.3 Process Managers / Sagas
- **Canon:** EIP Process Manager; Vernon RMPwAM ch.7; Khononov ch.9. Khononov distinguishes a *saga* (event-to-command mapping, simpler) from a *process manager* (a central state machine deciding the next step).
- **Mapping:**
  - A process manager **is** a (persistent, often sharded) actor with its own identity (`fulfillment-{orderId}`), timers (`IWithTimers`), and a pure transition function.
  - Side effects (sending `ReservePayment`) are driven by state in the shell. Every outbound command carries an **idempotency key**.
  - VERIFIED compile:

```csharp
public union FulfillmentState(AwaitingPayment, Fulfilled, Compensated);
public static class FulfillmentPolicy
{
    public const int MaxAttempts = 3;
    public static FulfillmentState Next(FulfillmentState s, object input) => (s, input) switch
    {
        (AwaitingPayment a, PaymentReserved)  => new Fulfilled(a.Order),
        (AwaitingPayment a, PaymentDeclined d) => new Compensated(a.Order, d.Reason),
        (AwaitingPayment a, PaymentTimedOut) when a.Attempts + 1 >= MaxAttempts => new Compensated(a.Order, "timeout"),
        (AwaitingPayment a, PaymentTimedOut)  => a with { Attempts = a.Attempts + 1 },
        _ => s,
    };
}
// Shell: ReceivePersistentActor, IWithTimers; Persist(e, Apply) then Resume() issues
// ReservePayment(order, amount, $"{order}-pay") and Timers.StartSingleTimer("pay-timeout", ...).
```

  - Caveat to fix in the book version: the sketch keeps the start data (`Total`) in constructor args. A real implementation must **persist the start event** (e.g. `FulfillmentStarted`) so recovery after rebalance has it. Also, inside sharding it must passivate rather than `Context.Stop(Self)`.
- **Triggering:** a projection on the `order-lifecycle` tag sends `OrderSubmitted` to the fulfillment shard region. This gives at-least-once delivery, so the process manager must dedupe the start.

### 5.4 Event Sourcing
- **Mapping:**
  - `ReceivePersistentActor` with `Persist`/`PersistAll` (atomic batch, VERIFIED compile) and `PersistAsync` (no stashing, higher throughput, but replies may interleave).
  - `DeferAsync` replies after the persist handlers (VERIFIED compile, used to reply `CommandAccepted`).
  - `SaveSnapshot`, `SnapshotOffer`, `RecoveryCompleted`.
- **Facts (SOURCED, event-sourcing page):**
  - Commands are stashed during `Persist`.
  - Persist failure → `OnPersistFailure` → the actor *stops*; use a backoff supervisor (sharding handles restarts on next message).
  - Rejection → `OnPersistRejected` → the actor continues.
  - `max-concurrent-recoveries = 50`.
  - `Sender` is dead letters during replay.
  - Don't use `PoisonPill` for persistent actors; use an explicit stop message.
  - Deleting events after a snapshot can break Persistence Query consumers.
- **Snapshot DTOs:** snapshot a **DTO**, not the union state (VERIFIED example `OrderSnapshot.From/ToState`), so snapshot schema is decoupled from in-memory types.
- **Versioning (Ops chapter):**
  - Event adapters (`IReadEventAdapter` for upcasting) and manifests.
  - Never rename persisted types or change `PersistenceId` formats without migration.
  - Each Akka serializer ID is permanent.

### 5.5 Outbox
- **Canon:** transactional outbox (Richardson). Khononov ch.9.
- **Mapping:** with event sourcing, **the journal *is* the outbox.** Events are atomically persisted, and a Persistence Query projection relays them to the broker with stored offsets. That gives at-least-once delivery, so consumers must be idempotent.
- **Pitfall:** `Persist(e, _ => kafkaProducer.Produce(...))`. That is a dual write: a crash between the two loses or duplicates the message, and it blocks the mailbox on I/O.
- For non-event-sourced (CRUD) supporting contexts, use a classic outbox table, or **Akka.Delivery's `EventSourcedProducerQueue`** (SOURCED: reliable-delivery page). It is a durable producer queue in the journal and can be sharded via `Akka.Cluster.Sharding.Delivery`.

### 5.6 Eventual consistency
- **Mapping:** between aggregates, between contexts, and between write and read models.
- **UX consequence:** HTTP endpoints return `202 Accepted` plus a resource location, or wait for the specific event. Teach "read your writes" via the aggregate's own state (ask the entity) versus read models (lagging).

### 5.7 Idempotency and delivery semantics
- **Facts:**
  - Akka's default is **at-most-once** delivery between actors (SOURCED, reliable-delivery page).
  - Sharding buffers during rebalance but does not guarantee delivery.
  - Akka.Delivery (`ProducerController`/`ConsumerController`, durable queue) gives ordered, buffered, redelivered delivery. Duplicates are possible after producer restart, which is effectively at-least-once (the docs don't use that term).
- **Domain mapping:**
  1. Natural idempotency via state, i.e. no-op transitions (VERIFIED example).
  2. Explicit dedup by command ID, kept as a bounded recent-ID set inside aggregate state and snapshotted.
  3. Idempotency keys on outbound commands from process managers.
  4. Idempotent projections that upsert by (persistenceId, sequenceNr).

---

## 6. Actor-specific mappings, anti-patterns and gotchas

### 6.1 The shell, verified (`Shell/OrderActor.cs`, trimmed)

```csharp
public sealed record OrderEnvelope(string OrderId, OrderCommand Command);
public sealed record CommandAccepted(int EventCount);
public sealed record CommandRejected(OrderError Error);

public sealed class OrderActor : ReceivePersistentActor
{
    private readonly TimeProvider _clock;
    private OrderState _state = Order.Initial;
    public override string PersistenceId { get; }

    public OrderActor(string orderId, TimeProvider clock)
    {
        PersistenceId = $"order-{orderId}";
        _clock = clock;
        Recover<OrderPlaced>(e => _state = Order.Evolve(_state, e));
        Recover<LineAdded>(e => _state = Order.Evolve(_state, e));
        Recover<OrderSubmitted>(e => _state = Order.Evolve(_state, e));
        Recover<OrderCancelled>(e => _state = Order.Evolve(_state, e));
        Recover<SnapshotOffer>(s => { if (s.Snapshot is OrderSnapshot snap) _state = snap.ToState(); });
        Command<OrderEnvelope>(env => Handle(env.Command));
        Command<GetOrderState>(_ => Sender.Tell(_state));
    }

    private void Handle(OrderCommand cmd)
    {
        switch (Order.Decide(_state, cmd, _clock))
        {
            case OrderError err: Sender.Tell(new CommandRejected(err)); break;
            case OrderEvent[] events when events.Length == 0: Sender.Tell(new CommandAccepted(0)); break;
            case OrderEvent[] events:
                var replyTo = Sender;
                PersistAll(events.Select(e => e.Value!), raw => _state = Order.Evolve(_state, ToEvent(raw)));
                DeferAsync(events.Length, n => replyTo.Tell(new CommandAccepted(n)));
                break;
        }
    }
}
```

Hosting, VERIFIED compile with Akka.Cluster.Hosting / Akka.Persistence.Hosting 1.5.73:

```csharp
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddAkka("sales", (akka, sp) =>
{
    akka.WithRemoting("localhost", 0)
        .WithClustering(new ClusterOptions { SeedNodes = ["akka.tcp://sales@localhost:0"], Roles = ["sales"] })
        .WithInMemoryJournal().WithInMemorySnapshotStore()
        .WithShardRegion<OrderActor>("order",
            (_, _, resolver) => id => OrderActor.PropsFor(id, sp.GetRequiredService<TimeProvider>()),
            new OrderMessageExtractor(),
            new ShardOptions { StateStoreMode = StateStoreMode.DData, Role = "sales" });
});
```

The TestKit test `Actor_persists_and_recovers_state` passes (VERIFIED). It places an order, gets a typed `CommandRejected(WrongState)` on a duplicate place, kills the actor, recreates it, and recovers `DraftOrder` from the journal.

### 6.2 Big gotcha: C# 15 unions and Akka 1.5 messaging and serialization (VERIFIED)

1. **Sending a union value sends the boxed wrapper struct, not the case.**
   - `TestActor.Tell((OrderCommand)new SubmitOrder(...))` arrives as an object of type `OrderCommand`, not `SubmitOrder`. Test `Sending_a_union_boxes_the_wrapper_not_the_case` passes.
   - So **`Receive<SubmitOrder>` will never match** if callers send the union.
   - **Rule:** either send the case records (and keep unions internal to the core), or wrap them in a typed envelope (`OrderEnvelope(string, OrderCommand)`) and switch inside.
2. **Newtonsoft (the Akka 1.5 default) silently drops union contents on deserialize.**
   - JSON written: `{"$type":"Sales.Domain.OrderCommand, Domain","Value":{"$type":"Sales.Domain.SubmitOrder, Domain",...}}`.
   - Read back: an `OrderCommand` with `Value == null`. There is **no exception**: silent data loss across remoting or persistence.
   - Test `Envelope_with_union_field_roundtrips` fails, demonstrating this.
3. **Newtonsoft cannot construct VOs that have only private constructors** (exception shown in 4.2).
4. **Detection:** `akka.actor.serialize-messages = on` in TestKit config forces every local message through serialization. The VERIFIED test `Serialize_messages_exposes_the_union_bug_locally` shows the envelope arriving with `Command.Value == null`. **The book must turn this on in all actor tests** (the 1.5 in-memory journal does not serialize).
5. **Fix options:**
   - (a) Envelopes and events with concrete case-typed fields plus a custom or protobuf serializer (manual DTO mapping in the shell). This is the book's main track on 1.5.
   - (b) A System.Text.Json-based custom Akka serializer with a union converter. UNVERIFIED whether STJ in .NET 11 has built-in union support; check before writing.
   - (c) Akka 1.6 `Akka.Serialization.V2`, which as of 1.6.0-beta5 supports C# 15 unions as field types (SOURCED, release notes; UNVERIFIED by compile).

### 6.3 Anti-pattern catalog (one sidebar each in the book)
- **Anemic actor:** setters as messages, rules elsewhere.
- **Logic in `Receive` handlers:** rules interleaved with `Persist`, `Sender.Tell`, logging. Untestable without TestKit and easy to break the "validate before persist" rule. The cure is the Decider.
- **Actor-per-row:** entities and VOs inside an aggregate made into actors.
- **Chatty asks / ask chains:** `Ask` inside actors to other aggregates. It blocks the mailbox if awaited (with `ReceiveAsync`) and leads to timeouts under load. Use Tell plus a process manager.
- **IActorRef as identity:** storing refs in persistent state. Refs don't survive restart or rebalance; store IDs instead.
- **God supervisor / deep hierarchies** pretending to be aggregates.
- **Dual writes** in persist callbacks (see 5.5).
- **Mutable messages:** `List<T>` in messages. Use `ImmutableList`/arrays treated as read-only, and records.
- **Persisting the union wrapper** (see 6.2).
- **Changing `maxNumberOfShards`, the `PersistenceId` format, the sharding `typeName`, or serializer IDs** in production.
- **`Context.Stop(Self)` in sharded entities** (SOURCED: message loss; use Passivate).
- **Using `PoisonPill` to stop persistent actors** (SOURCED: it bypasses stashed commands).
- **Non-determinism in Evolve:** reading the clock or random during replay. All non-determinism belongs in Decide via injected providers and is captured in events.

### 6.4 Where the mapping is awkward (be honest in the book)
- **Cross-aggregate invariants** (e.g. unique usernames). Actors can't do set validation across entities. Options:
  - a dedicated "reservation" aggregate keyed by the unique value (actor-friendly);
  - a DB unique constraint in a supporting service;
  - accepting and compensating.
- **Large or unbounded aggregates** (a long-lived account with millions of events): snapshots, or time-scoped aggregates per the canvas "Size" advice.
- **Hot aggregates** (a single popular auction or inventory SKU): one mailbox is a throughput ceiling. Split the aggregate (sub-aggregates per bucket) or relax invariants with corrective policies (canvas).
- **Queries:** actors are poor at ad hoc queries. Always use CQRS read models.
- **Bounded-context-per-cluster vs shared cluster:** shared clusters couple deploys and the wire protocol. Recommend a cluster per context for core contexts.
- **Repository abstraction:** there is no `Load`/`Save` to mock. Testing goes through Decider tests plus TestKit, not a fake repository.

---

## 7. Testing strategy implied by the mapping (for the unit-testing chapter)

1. **Decider tests (most tests):** Given (event history folded with Evolve) / When (command) / Then (events or error). Pure, fast, xUnit v3. VERIFIED: 3 tests in `OrderDecideTests`. Add property-based tests:
   - Evolve is total for every event sequence Decide can produce.
   - Decide never emits events that Evolve ignores.
2. **Translation tests:** ACL `Translate` and projection mappers are pure functions.
3. **Actor shell tests:** Akka.TestKit.Xunit (v3) or Akka.Hosting.TestKit, with `serialize-messages = on`. These cover reply protocol, persistence/recovery round-trip, snapshot recovery and passivation.
   - **Packaging gotcha (VERIFIED):** `Akka.Hosting.TestKit` 1.5.73 pulls in xUnit **v3**. Mixing it with `xunit` 2.9.3 / `Akka.TestKit.Xunit2` gives `CS0433: The type 'FactAttribute' exists in both xunit.core 2.9.3 and xunit.v3.core 3.2.2`.
   - **Standardize on xunit.v3 + Akka.TestKit.Xunit.** Test projects need `<OutputType>Exe</OutputType>`.
   - Expect xUnit1051 analyzer warnings, which ask you to pass `TestContext.Current.CancellationToken`.
4. **Serialization contract tests:** round-trip every persisted and wire type through the configured serializer, and keep golden files of old event JSON/bytes to catch breaking changes. This bridges into the versioning chapter.
5. **Multi-node / sharding tests:** Akka.MultiNode.TestAdapter or Aspire/Testcontainers integration tests (UNVERIFIED current package status).

---

## 8. Proposed coverage checklist (for the editor; tick off per chapter)

**Strategic:**
- [ ] Domain
- [ ] Subdomains: core / supporting / generic, plus BCC business role and evolution
- [ ] Ubiquitous Language
- [ ] Bounded Context
- [ ] Context Map
- [ ] Partnership
- [ ] Shared Kernel
- [ ] Customer/Supplier
- [ ] Conformist
- [ ] ACL
- [ ] OHS
- [ ] Published Language
- [ ] Separate Ways
- [ ] Big Ball of Mud
- [ ] Team relationships (Mutually Dependent / Upstream-Downstream / Free)
- [ ] Distillation: Core Domain, Generic Subdomains, Domain Vision Statement, Highlighted Core, Cohesive Mechanisms, Segregated Core, Abstract Core
- [ ] Large-scale structure: Evolving Order, System Metaphor, Responsibility Layers, Knowledge Level, Pluggable Component Framework
- [ ] Model-Driven Design
- [ ] Hands-on Modelers
- [ ] Refactoring Toward Deeper Insight
- [ ] Continuous Integration

**Discovery:**
- [ ] EventStorming (Big Picture / Process / Design Level)
- [ ] Domain Storytelling
- [ ] Bounded Context Canvas V5
- [ ] Aggregate Design Canvas
- [ ] Context mapping workshop

**Tactical:**
- [ ] Entities
- [ ] Value Objects (smart constructors, unions)
- [ ] Aggregates and Vernon's 4 rules
- [ ] Invariants
- [ ] Domain Events
- [ ] Factories
- [ ] Repositories
- [ ] Domain Services
- [ ] Application Services
- [ ] Modules
- [ ] Specifications
- [ ] Supple design: Intention-Revealing Interfaces
- [ ] Supple design: Side-Effect-Free Functions
- [ ] Supple design: Assertions
- [ ] Supple design: Standalone Classes
- [ ] Supple design: Closure of Operations
- [ ] Supple design: Declarative Design
- [ ] Supple design: Conceptual Contours
- [ ] Supple design: Established Formalisms
- [ ] Making implicit concepts explicit (constraints, processes, policies/strategy)

**Architectural:**
- [ ] Layered / Hexagonal / Clean
- [ ] CQRS
- [ ] Event Sourcing (snapshots, upcasting)
- [ ] Process Manager vs Saga
- [ ] Domain vs Integration events
- [ ] Outbox (journal-as-outbox, Akka.Delivery)
- [ ] Eventual consistency and UX
- [ ] Idempotency and delivery semantics
- [ ] Business-logic pattern choice (Khononov: transaction script / active record / domain model / event-sourced domain model)

**Actor-specific:**
- [ ] Aggregate = sharded persistent actor
- [ ] Shard region as repository
- [ ] Message extractor
- [ ] Passivation
- [ ] Remember-entities
- [ ] Process manager actor with timers
- [ ] ACL translating actor
- [ ] Projections via Persistence Query
- [ ] Split-brain resolver and the single-writer guarantee
- [ ] Serialization strategy and serializer IDs
- [ ] Anti-pattern catalog
- [ ] Functional core / actor shell

---

## 9. Open items (UNVERIFIED, for other research agents)

- Whether System.Text.Json in .NET 11 serializes C# 15 unions natively. This is decisive for the 1.5 serializer recommendation.
- Whether Akka 1.6.0-beta5 Serialization V2 with unions compiles on .NET 11 with `net11.0` TFM projects (1.6 packages target `net10.0`).
- `ShardedDaemonProcess` API shape in 1.5.73 for running projections.
- Akka.Persistence.Sql 1.5.73 tag-table mode and `EventsByTag` offset semantics.
- xunit.v3 4.x compatibility with Akka.TestKit.Xunit 1.5.73 (we used 3.2.2).
- Current versions of property-testing libraries (CsCheck/FsCheck) and architecture-test libraries.
- The `Akka.Management.Cluster.Bootstrap` NuGet id returned only 0.3.0-beta4. The real package is probably `Akka.Management` (1.5.73, which includes bootstrap). Confirm.
