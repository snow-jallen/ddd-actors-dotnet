# Research notes: Akka.NET as of 2026-11-04

Scope: the state of Akka.NET for a book on DDD with actors on .NET 11. Covers versions, 1.5 vs 1.6, the idiomatic APIs we will teach, and the gotchas we found.

Method: NuGet versions, publish dates and TFMs come from the NuGet v3 flat-container and registration APIs. Method signatures come from reflection over the restored 1.5.73 assemblies. Every code block marked **[VERIFIED]** was compiled with .NET SDK `11.0.100-rc.3.26522.148` targeting `net11.0`, and most were also run. The spike projects are in `research/akka-core-verify/` (see its README). Anything marked **UNVERIFIED** comes only from docs or memory.

---

## 0. TL;DR for the editor

1. **Target Akka.NET 1.5.73 (stable) for the book and sample repo.** Our whole sample (sharding, SQL persistence, query, projections, timers, stash, backoff, coordinated shutdown) also compiled and ran on **1.6.0-beta5** without source changes. So the 1.5 to 1.6 upgrade can be a chapter in the operations/versioning part ("upgrading a live event-sourced system"), and the main line doesn't need to depend on a beta.
2. **Hosting versions are now lock-step.** Since 1.5.72, `Akka.Hosting`, `Akka.Cluster.Hosting`, `Akka.Remote.Hosting`, `Akka.Persistence.Hosting` and `Akka.Hosting.TestKit` ship from the akka.net repo at the same version as `Akka`. Pin every `Akka.*` package to `1.5.73`. `Akka.Persistence.Sql(.Hosting)`, `Akka.Management` and `Akka.Discovery.*` are also at 1.5.73.
3. **`Akka.Persistence.PostgreSql`, `.SqlServer` and `.Sqlite` (Sql.Common) are deprecated.** NuGet marks them "Legacy" and points to `Akka.Persistence.Sql`. Use `Akka.Persistence.Sql.Hosting` 1.5.73 (Linq2Db 6.5.1).
4. **C# 15 unions and Akka don't mix naively.** These were verified:
   - A boxed union is the union *struct*: `Tell(pet)` matches `Receive<Pet>`, not `Receive<Cat>`.
   - Akka's default Newtonsoft serializer *silently* deserializes a union with `Value == null`. There is no exception and the data is lost.
   - Use unions **inside** the domain model (decide/apply, results). Persist and send the **case records**.
   - On 1.6, `Akka.Serialization.V2` supports union fields natively (verified on beta5).
5. **Serialization:** the 1.5 default for user types is still Newtonsoft.Json, with assembly-qualified `$type` names written into the journal. The book should introduce a `SerializerWithStringManifest` early (System.Text.Json with explicit manifests, verified) and register it with `WithCustomSerializer`.
6. **Sharding default trap:** `akka.cluster.sharding.state-store-mode` still defaults to `persistence`, and the docs say to use `ddata`. Always set `ShardOptions.StateStoreMode = StateStoreMode.DData`.
7. **Defaults worth teaching (verified effective config):**
   - Split brain resolver (SBR): `keep-majority`, `stable-after = 20s`, `down-all-when-unstable = on`.
   - `passivate-idle-entity-after = 120s`.
   - `ask-timeout = infinite`, so always pass a timeout.
   - `akka.cluster.app-version = assembly-version`.

---

## 1. Exact versions (NuGet, queried 2026-11-04)

| Package | Latest stable | Latest pre | Stable TFMs | Notes |
|---|---|---|---|---|
| Akka | **1.5.73** (2026-10-05) | 1.6.0-beta5 (2026-11-04) | net6.0, netstandard2.0 | 1.6 betas are **net10.0 only** |
| Akka.Hosting | **1.5.73** | 1.6.0-beta5 | net6.0, netstandard2.0 | ships from akka.net repo since 1.5.72; depends on OpenTelemetry 1.12+/OpenTelemetry.Api 1.15.3+ |
| Akka.Remote.Hosting | 1.5.73 | 1.6.0-beta5 | same | |
| Akka.Cluster.Hosting | 1.5.73 | 1.6.0-beta5 | same | brings Akka.Cluster.Sharding/Tools |
| Akka.Persistence.Hosting | 1.5.73 | 1.6.0-beta5 | same | |
| Akka.Persistence.Sql | 1.5.73 (2026-11-02) | 1.6.0-beta5 | net6.0, netstandard2.0 | Linq2Db 6.5.1, LanguageExt.Core 4.4.9 |
| Akka.Persistence.Sql.Hosting | **1.5.73** | 1.6.0-beta5 | same | |
| Akka.Persistence.PostgreSql | 1.5.37 (2025-01) | — | | **Deprecated ("Legacy")**, alt = Akka.Persistence.Sql |
| Akka.Persistence.PostgreSql.Hosting | 1.5.37 | — | | legacy |
| Akka.Persistence.Sql.Common / Query.Sql / Sqlite | 1.5.39 | — | | legacy (retired March 2025) |
| Akka.Cluster.Sharding | 1.5.73 | 1.6.0-beta5 | | |
| Akka.Streams | 1.5.73 | 1.6.0-beta5 | | |
| Akka.Persistence / .Query | 1.5.73 | 1.6.0-beta5 | | |
| Akka.Persistence.Query.InMemory | 1.5.73 | 1.6.0-beta5 | | |
| Akka.Management (includes Cluster.Bootstrap) | 1.5.73 | 1.6.0-beta5 | | standalone `Akka.Management.Cluster.Bootstrap` pkg is an old 0.3.0-beta4; don't use it |
| Akka.Discovery.KubernetesApi | 1.5.73 | 1.6.0-beta5 | | |
| Akka.Coordination.KubernetesApi (lease) | 1.5.73 | 1.6.0-beta5 | | |
| Akka.Discovery.Azure / .Redis | 1.5.73 | 1.6.0-beta5 | | |
| Akka.Aspire / Akka.Aspire.Hosting | 1.5.73 | 1.6.0-beta5 | | Aspire AppHost integration (Sep 2026) |
| Akka.Persistence.Azure(.Hosting) | 1.5.71 | 1.6.0-beta3 | | |
| Akka.Persistence.Redis | 1.5.68 | 1.6.0-beta3 | | |
| Akka.DependencyInjection | 1.5.73 | 1.6.0-beta5 | | pulled in by Akka.Hosting |
| Akka.Serialization.Hyperion | 1.5.73 | 1.6.0-beta5 | | Hyperion lib itself 0.12.2, repo quiet since 2025-05 |
| Akka.Serialization.V2 | — | 1.6.0-beta5 | net10.0 | **1.6 only**, source-generated MessagePack |
| Akka.Persistence.Embedded(.Hosting) | — | 1.6.0-beta5 | | **1.6 only** (UNVERIFIED what it is: presumably an embedded/local journal) |
| Akka.TestKit.Xunit (xunit **v3**) | 1.5.73 | 1.6.0-beta5 | | |
| Akka.TestKit.Xunit2 (xunit v2) | 1.5.73 | 1.6.0-beta5 | | |
| Akka.Hosting.TestKit | 1.5.73 | 1.6.0-beta5 | | depends on Akka.TestKit.Xunit + xunit.v3.extensibility.core 3.2.2 |
| Akka.Persistence.TestKit(.Xunit / .Xunit2) | 1.5.73 | 1.6.0-beta5 | | |
| Akka.Analyzers | 0.3.3 | — | | auto-referenced by Akka 1.5 |
| Akka.Streams.Kafka | 1.5.73 | — | | |
| Akka.Logger.Serilog | 1.5.69 | — | | |
| Akka.HealthCheck.Hosting | 1.5.37 | — | | superseded by built-in Akka.Hosting health checks |
| Akka.Cluster.Chunking | 0.4.0 | — | | |
| Petabridge.Cmd.Host | 1.5.2 | — | | |
| Phobos | — | — | | commercial; not on nuget.org as `Phobos.Actor` |

Supporting packages used in the verified spikes:
- `Microsoft.Extensions.Hosting` 10.0.14 (11.0.0-rc.3 also exists)
- `Microsoft.Data.Sqlite` 10.0.14
- `linq2db` 6.5.1
- `xunit.v3` 3.2.2 (4.0.2 is latest; Akka.Hosting.TestKit was built against 3.2.2, so 3.2.2 is the safe pin)
- `xunit.runner.visualstudio` 3.1.5
- `Microsoft.NET.Test.Sdk` 18.11.0

**.NET 11 support.** No Akka 1.5 package targets net10/net11. `net11.0` apps consume the `net6.0` assets fine (verified: build and run). 1.6 betas target `net10.0` and also work from `net11.0` (verified). 1.5.73 rolled the Microsoft.Extensions minimums back down: `[6.0,)` for core and `[9.0.0,)` for Hosting. 1.5.72 had briefly required 10.0.

**Verified csproj (1.5.73 on net11.0):**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net11.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Akka.Hosting" Version="1.5.73" />
    <PackageReference Include="Akka.Cluster.Hosting" Version="1.5.73" />
    <PackageReference Include="Akka.Persistence.Hosting" Version="1.5.73" />
    <PackageReference Include="Akka.Persistence.Sql.Hosting" Version="1.5.73" />
    <PackageReference Include="Akka.Analyzers" Version="0.3.3" />
    <PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.14" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.14" />
  </ItemGroup>
</Project>
```

Recommendation for the sample repo: use Central Package Management (`Directory.Packages.props`) with a single `<AkkaVersion>1.5.73</AkkaVersion>` property. Lock-step versioning makes that natural. The 1.6 upgrade then becomes a one-line diff, which is a good teaching moment for the versioning chapter.

---

## 2. 1.5.x vs 1.6: what changed, and what the book should target

Sources:
- GitHub releases 1.6.0-beta1..beta5: https://github.com/akkadotnet/akka.net/releases
- https://github.com/akkadotnet/akka.net/blob/dev/GETTING_STARTED_V1.6.md
- https://github.com/akkadotnet/akka.net/blob/dev/BREAKING_CHANGES_V1.6.md
- Petabridge roadmap (Jan 2025): https://petabridge.com/blog/akkadotnet-v1.6-roadmap/

### Release history

- **1.6.0-beta1** shipped on 2026-10-02.
- **beta2** and **beta3** followed on 2026-10-06 and 10-07.
- **beta4** shipped on 10-28 and **beta5** on 11-04, today.

The GitHub milestone "1.6.0" has **no due date**, with 53 open and 90 closed issues. A "1.5.74" milestone is open, so the 1.5 line is still maintained. The Petabridge roadmap had targeted early Q1 2026 for completion, so the schedule slipped. The getakka.net website is "frozen on v1.5 until v1.6 stabilizes" (GETTING_STARTED_V1.6.md).

### What's in 1.6 (from the getting-started guide and release notes)

- **.NET 10 only.** All packages target `net10.0`; netstandard2.0, net6, net8 and net48 are dropped. `Microsoft.Extensions.*` minimum is 10.0.
- **Akka.Hosting ships in lock-step from the main repo.** This was actually backported in 1.5.72. Under 1.6 it changes so that extensions register via `ExtensionsSetup` and no longer appear in `akka.extensions` HOCON.
- **Artery TCP remoting.** It is experimental and off by default. Addresses become `akka://Sys@host:25520` instead of `akka.tcp://...:2552`, and it is not wire-compatible with classic. There is no Hosting helper yet, so it is HOCON only. The roadmap's QUIC transport hasn't landed; TCP is the only implemented Artery transport.
- **Akka.Serialization.V2.** A Roslyn source generator for MessagePack-backed serializers, using `[AkkaSerializable(Manifest=..)]`, `[AkkaField(n)]` and `[AkkaSerializer<TProtocol>(name, id)] partial class X : AkkaSerializer`.
  - It is AOT-safe and needs no reflection.
  - **beta5 added C# 15 union support** (field types that are a struct implementing `IUnion`) and inferred unions over interfaces and abstract hierarchies.
  - The API is still churning: `[AkkaUnion]` was removed in beta5 and `[AkkaSerializable<T>]` was renamed to `[AkkaSerializerIncludes<T>]`.
- **Native AOT.** CI covers local actor systems, Akka.Hosting and Akka.Persistence (through the Hosting builders). Cluster isn't supported under AOT, and classic remoting doesn't work under AOT.
- **`Akka.IO.ByteString` removed**, replaced by `ReadOnlySequence<byte>` and `ReadOnlyMemory<byte>`.
- **HOCON from App.config removed.**
- **Behavior and wire changes relevant to an operations chapter** (from BREAKING_CHANGES_V1.6.md):
  - **#8702:** first-party module serializers are registered by default in code. Persisted events and snapshots of `string`, `int`, `long`, `IActorRef`, `Status.*` and similar types may change serializer (e.g. `primitive` id 17, `akka-misc` id 16). Old data still reads, but **a 1.5 node can't read the new rows** unless it loads the matching config. This is a textbook rolling-upgrade and versioning hazard.
  - **#8445:** sharding `least-shard-allocation-strategy.rebalance-absolute-limit` default changes from 0 to 20, which selects the new bounded rebalance algorithm.
  - **#8359:** `Cluster.Get()` no longer blocks until cluster core start.
  - **#8317:** system UIDs widen to `long`. The wire format is unchanged unless you enable `use-64bit-system-uids`.
  - **#8545:** `Akka.TestKit.Xunit.TestKit` implements `IAsyncLifetime` with virtual methods. Derived specs that declare their own must mark them `override`. This was also backported to 1.5.72.
  - **#8886:** disposing the shared default materializer no longer shuts it down.
  - **#8263:** `Source.ActorRef` ignores `PoisonPill`; use `Status.Success` to complete the stream.
- **Third-party plugins** built against 1.5 "can hit a binary break on 1.6. Wait for its own 1.6 build."

### Recommendation: target 1.5.73, with a 1.6 upgrade chapter or appendix

Reasoning:
1. **Stability.** 1.6 is a beta with a weekly cadence, and its flagship serialization API was still renaming attributes in the latest beta. A book's code has to survive months on a shelf.
2. **Docs alignment.** getakka.net documents 1.5. Readers who follow links will see 1.5 APIs.
3. **The sample is source-compatible.** We verified that the full sample compiles and runs on 1.6.0-beta5 with only the version numbers changed (`research/akka-core-verify/Verify16`). Choosing 1.5 costs the reader nothing.
4. **Ecosystem.** Persistence.Sql, Management, Discovery and Aspire all have 1.6 betas, but Azure and Redis persistence are a beta behind (beta3).
5. **Pedagogical upside.** "Upgrade from Akka 1.5 to 1.6 in a running, event-sourced cluster" is a realistic ops scenario. It covers wire compatibility, the serializer changes in #8702 and `app-version`-driven rolling updates. It belongs in the releases and versioning chapter.
6. **Show 1.6 Serialization.V2 as the forward path for unions.** Use it in a sidebar or appendix, because it is the only Akka serializer that round-trips C# unions out of the box.

Revisit before publication: if `1.6.0` GA ships before the book freezes, switching is low-risk. Re-run the Verify16 spike to confirm.

---

## 3. Actors: idiomatic core APIs (1.5.73)

### ReceiveActor vs UntypedActor

- **`ReceiveActor`** is the idiomatic C# style: typed handlers registered in the constructor. Handlers are matched **in registration order, first match wins**. The docs say: "If more than one handler matches, the one that appears first is used". Use `Become(Action)` and `BecomeStacked` for behaviors.
- **`UntypedActor`** overrides `OnReceive(object message)`. With C# pattern matching this is a perfectly good style and slightly cheaper. It pairs naturally with `switch` over closed hierarchies. Unhandled messages go to `Unhandled(message)`.
- For persistence the equivalents are **`ReceivePersistentActor`** (`Command<T>`, `Recover<T>`) and **`UntypedPersistentActor`** (`OnCommand`, `OnRecover`).
- Book suggestion: teach `ReceiveActor`/`ReceivePersistentActor` as the main style, because readers will find it in the docs. Show the `switch`-based `UntypedActor` once, as it reads well with unions and exhaustive switches.

### `ReceiveAsync` semantics [VERIFIED]

`ReceiveAsync<T>` **suspends the mailbox until the task completes**. There is no interleaving, and `Sender` stays the same across `await`.

Verified output for three concurrent `Ask`s:

```
start 1 | end 1 senderStable=True | start 2 | end 2 senderStable=True | start 3 | ...
```

So async handlers are safe but serialize throughput. For long I/O, prefer `PipeTo(Self)` with a "loading" behavior and stash (example below). Petabridge's "Managing Long-Running Tasks Inside Akka.NET Actors" (2026-04-13) covers this.

### Tell vs Ask

- **`Tell` is the default.** Use `Ask<T>(msg, timeout)` only at the boundary, for example a request from ASP.NET or a test, or a deliberate request/response with a timeout.
- **Always pass a timeout.** The effective default `akka.actor.ask-timeout = infinite` (verified config dump). `WithActorAskTimeout(TimeSpan)` exists on the Hosting builder.
- Analyzer **AK2000** errors on `Ask` with `TimeSpan.Zero`.
- Ask creates a temp actor per call. Inside actors, prefer Tell plus correlation, or `PipeTo`.

### Akka.Analyzers rules (auto-referenced; https://getakka.net/articles/debugging/akka-analyzers.html)

| Rule | What it catches | Severity |
|---|---|---|
| AK1000 | `new` on an actor | error |
| AK1002 | `await Self.GracefulStop()` in `ReceiveAsync` | error |
| AK1003 | `ReceiveAsync` without an async body | warning |
| AK1004 | `ScheduleTellRepeatedly` leaks | warning |
| AK1005 | closures must capture `Sender`/`Self` | warning |
| AK1006 | `Persist` in a loop; use `PersistAll` | warning |
| AK1007 | timers in `PreRestart` | error |
| AK1008 | `ActorSystem.ActorOf` inside an actor | warning |
| AK2000 | Ask with a zero timeout | error |
| AK2001 | handling auto-handled messages (`StartEntity`, `ShardingEnvelope`) in an `IMessageExtractor` | warning |
| AK2002 | repeated `Context.Materializer()` | warning |
| AK2003–AK2005 | async-void `Receive`/`Command` handlers | error |

We actually tripped **AK2001** in the spike by mapping `ShardRegion.StartEntity` in our extractor. That handling has been automatic since 1.5.15.

### Timers, stash, supervision, backoff [VERIFIED compiles and runs]

```csharp
public sealed class PriceCacheActor : ReceiveActor, IWithTimers, IWithStash
{
    public sealed record GetPrice(string Sku);
    public sealed record Price(string Sku, decimal Amount);
    private sealed record Loaded(IReadOnlyDictionary<string, decimal> Prices);
    private sealed record Refresh { public static readonly Refresh Instance = new(); }

    private IReadOnlyDictionary<string, decimal> _prices = new Dictionary<string, decimal>();
    public ITimerScheduler Timers { get; set; } = null!;   // injected by Akka
    public IStash Stash { get; set; } = null!;             // injected by Akka

    public PriceCacheActor() => Loading();

    protected override void PreStart()
    {
        Timers.StartPeriodicTimer("refresh", Refresh.Instance, TimeSpan.FromMinutes(5));
        Self.Tell(Refresh.Instance);
    }

    private void Loading()
    {
        Receive<Refresh>(_ => LoadAsync().PipeTo(Self));
        Receive<Loaded>(l => { _prices = l.Prices; Stash.UnstashAll(); Become(Ready); });
        ReceiveAny(_ => Stash.Stash());
    }

    private void Ready()
    {
        Receive<GetPrice>(q => Sender.Tell(new Price(q.Sku, _prices.GetValueOrDefault(q.Sku))));
        Receive<Refresh>(_ => { Become(Loading); Self.Tell(Refresh.Instance); });
    }

    private static async Task<Loaded> LoadAsync()
    {
        await Task.Delay(10);
        return new Loaded(new Dictionary<string, decimal> { ["sku-1"] = 9.99m });
    }

    protected override SupervisorStrategy SupervisorStrategy() =>
        new OneForOneStrategy(maxNrOfRetries: 3, withinTimeRange: TimeSpan.FromMinutes(1), localOnlyDecider: ex => ex switch
        {
            ArgumentException => Directive.Resume,
            TimeoutException  => Directive.Restart,
            _                 => Directive.Escalate
        });
}

public static class BackoffExample
{
    public static Props Wrap(Props child) =>
        BackoffSupervisor.Props(
            Backoff.OnFailure(child, "price-cache",
                    minBackoff: TimeSpan.FromSeconds(1),
                    maxBackoff: TimeSpan.FromSeconds(30),
                    randomFactor: 0.2,
                    maxNrOfRetries: -1)
                .WithAutoReset(TimeSpan.FromSeconds(10)));
}
```

Notes:
- Timer keys replace previous timers with the same key, and timers are cancelled automatically when the actor stops. Prefer them over `ScheduleTellRepeatedly` (AK1004).
- `IWithStash` compiles in 1.5.73. `IWithUnboundedStash` and `IWithBoundedStash` also exist (UNVERIFIED nuance: `IWithStash` is the general alias).
- Backoff API in 1.5.73 is `Akka.Pattern.Backoff.OnFailure/OnStop(...)` returning `BackoffOptions`, with `.WithAutoReset`, `.WithManualReset`, `.WithSupervisorStrategy`, `.WithReplyWhileStopped`, `.WithMaxNrOfRetries` and `.WithFinalStopMessage`. Then call `BackoffSupervisor.Props(options)`. There's no JVM-style `BackoffOpts` type in .NET. The build showed no obsolete warnings.
- The default supervisor strategy is OneForOne with Restart for most exceptions. `ActorInitializationException` and `ActorKilledException` stop the actor (standard Akka semantics; UNVERIFIED for .NET specifics).
- For streams, use `RestartSource.OnFailuresWithBackoff(..., RestartSettings.Create(min, max, randomFactor))` (verified, see §7).

---

## 4. Akka.Persistence: ReceivePersistentActor (1.5.73)

### API surface (from reflection over Akka.Persistence 1.5.73)

**`Eventsourced` (base of all persistent actors):**

```
Persist<TEvent>(TEvent, Action<TEvent>)               Persist<TEvent>(TEvent, Func<TEvent,Task>)
PersistAll<TEvent>(IEnumerable<TEvent>, Action<TEvent> | Func<TEvent,Task>
                   [, Action onComplete | Func<Task> onCompleteAsync])
PersistAsync<TEvent>(...)   PersistAllAsync<TEvent>(... same overload family incl. onComplete)
DeferAsync<TEvent>(TEvent, Action<TEvent> | Func<TEvent,Task>)
SaveSnapshot(object)  DeleteSnapshot(long)  DeleteSnapshots(SnapshotSelectionCriteria)
DeleteMessages(long toSequenceNr)  LoadSnapshot(string pid, SnapshotSelectionCriteria, long toSeqNr)
protected OnPersistFailure(Exception, object evt, long seqNr)   // actor is stopped after this
protected OnPersistRejected(Exception, object evt, long seqNr)  // e.g. serialization failure; actor keeps running
protected OnRecoveryFailure(Exception, object message)
LastSequenceNr, SnapshotSequenceNr, PersistenceId (abstract), IsRecovering, ...
```

**`ReceivePersistentActor`:**
- `Command<T>(Action<T>)` (with optional predicate), `CommandAsync<T>(Func<T,Task>)`, `CommandAny`
- `Recover<T>(Action<T>)`, `RecoverAny`
- `Become`/`BecomeStacked`

The async handler overloads and the `onComplete` overloads of `PersistAll` arrived in **1.5.57**: "Migrating from CRUD to CQRS and Event-Sourcing with Akka.Persistence", https://petabridge.com/blog/crud-to-cqrs-akka-persistence/ (Dec 2025).

Semantics to teach:
- **`Persist` / `PersistAll`** stash incoming commands until the handler(s) run. This gives strict consistency.
- **`PersistAsync`** doesn't stash, giving higher throughput, but commands interleave with handlers.
- **`DeferAsync`** runs a handler after the preceding `PersistAsync`s complete.
- **AK1006:** use `PersistAll` for multiple events.
- **Snapshots** are never automatic. Call `SaveSnapshot` yourself. Results come back as `SaveSnapshotSuccess`/`SaveSnapshotFailure` messages, and deletions as `DeleteSnapshotsSuccess/Failure` and `DeleteMessagesSuccess/Failure`.
- **Recovery** runs `SnapshotOffer` first (if any), then the remaining events, then `RecoveryCompleted`.

### Verified aggregate actor (runs under sharding with SQLite; recovers from snapshot + events)

```csharp
public sealed class OrderActor : ReceivePersistentActor
{
    private const int SnapshotEvery = 3;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private OrderState _state;

    public override string PersistenceId { get; }

    public OrderActor(string orderId)
    {
        PersistenceId = $"order-{orderId}";
        _state = OrderState.Empty(orderId);

        // ---- recovery ----
        Recover<SnapshotOffer>(offer => { if (offer.Snapshot is OrderState s) _state = s; });
        Recover<IOrderEvent>(e => _state = _state.Apply(OrderState.ToUnion(e)));
        Recover<RecoveryCompleted>(_ => _log.Info("{0} recovered at seqNr {1}", PersistenceId, LastSequenceNr));

        // ---- commands ----
        Command<GetOrder>(_ => Sender.Tell(View()));
        Command<IOrderCommand>(HandleCommand);

        // ---- snapshot housekeeping ----
        Command<SaveSnapshotSuccess>(ok => DeleteSnapshots(new SnapshotSelectionCriteria(ok.Metadata.SequenceNr - 1)));
        Command<SaveSnapshotFailure>(f => _log.Warning(f.Cause, "snapshot failed"));
        Command<DeleteSnapshotsSuccess>(_ => { });
        Command<DeleteSnapshotsFailure>(f => _log.Warning(f.Cause, "delete snapshots failed"));
    }

    private void HandleCommand(IOrderCommand cmd)
    {
        switch (_state.Decide(cmd))            // Decide returns the union Decision(Accepted, Rejected)
        {
            case Rejected r:
                Sender.Tell(new CommandRejected(cmd.OrderId, r.Reason));
                break;
            case Accepted a:
                var replyTo = Sender;
                // Persist the CASE value (a plain record), never the union struct itself.
                PersistAll(a.Events.Select(e => (IOrderEvent)e.Value!), evt =>
                {
                    _state = _state.Apply(OrderState.ToUnion(evt));
                    if (LastSequenceNr % SnapshotEvery == 0) SaveSnapshot(_state);
                }, onComplete: () => replyTo.Tell(new CommandAccepted(cmd.OrderId, LastSequenceNr)));
                break;
        }
    }

    private OrderView View() => new(_state.OrderId, _state.Status.ToString(), _state.Total, _state.Lines.Count);

    protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
    {
        _log.Error(cause, "persist rejected for {0} at {1}", @event.GetType().Name, sequenceNr);
        base.OnPersistRejected(cause, @event, sequenceNr);
    }
}
```

Observed run output:
- Commands replied `CommandAccepted{Version=1..4}` and one `CommandRejected{quantity must be positive}`.
- After a full host restart: `recovered at seqNr 4`, with `OrderView{Status=Submitted, Total=24.98, Lines=2}`. The snapshot row sat at seq 3, and event 4 was replayed.
- Handler ordering matters. `Command<GetOrder>` is registered before `Command<IOrderCommand>` because `GetOrder : IOrderCommand`.

The domain side, verified, is the "functional core" the DDD chapters can build on:

```csharp
public union OrderEvent(OrderPlaced, ItemAdded, OrderSubmitted);
public sealed record Accepted(ImmutableArray<OrderEvent> Events);
public sealed record Rejected(string Reason);
public union Decision(Accepted, Rejected);

public OrderState Apply(OrderEvent e) => e switch
{
    OrderPlaced p  => this with { Status = OrderStatus.Open, CustomerId = p.CustomerId },
    ItemAdded i    => this with { Lines = Lines.Add(new OrderLine(i.Sku, i.Quantity, i.UnitPrice)) },
    OrderSubmitted => this with { Status = OrderStatus.Submitted },
    null           => this,   // default(OrderEvent).Value is null; without this arm: CS8655 warning
};
```

### Tagging via a write event adapter [VERIFIED]

```csharp
public sealed class OrderTaggingAdapter : IWriteEventAdapter
{
    public string Manifest(object evt) => string.Empty;
    public object ToJournal(object evt) => evt switch
    {
        IOrderEvent e => new Tagged(e, ["order", $"order-{e.GetType().Name}"]),
        _ => evt
    };
}
// registration (Akka.Persistence.Hosting): journalBuilder: j => j.AddWriteEventAdapter<OrderTaggingAdapter>("order-tagger", [typeof(IOrderEvent)])
```

`AkkaPersistenceJournalBuilder` also has `AddEventAdapter<T>` (read and write), `AddReadEventAdapter<T>` (useful for **event upcasting**, which the versioning chapter needs), `WithHealthCheck(...)` and `WithCustomHealthCheck(...)`. Akka.Persistence.Sql wrote the tags into a separate `tags` table, `(ordering_id, tag, sequence_nr, persistence_id)`. That is the "TagTable" mode. With `autoInitialize: true` on a fresh SQLite database, it created the tables `journal`, `tags` and `snapshot`; there was no tags column in `journal`.

---

## 5. C# 15 unions with Akka: verified findings

Union probe (`research/akka-core-verify/UnionAkka`, Akka 1.5.73):

| Experiment | Result |
|---|---|
| `object boxed = (Pet)new Cat("Tom")`, then `boxed.GetType()` | `Pet` (a value type implementing `System.Runtime.CompilerServices.IUnion`) |
| `boxed is Cat` | **false** |
| `actor.Ask(pet)` with handlers `Receive<Cat>` and `Receive<Pet>` | `Receive<Pet>` handled it |
| `actor.Ask(pet.Value!)` | `Receive<Cat>` handled it |
| Newtonsoft (Akka's default `json` serializer) on `Pet` or `Adopted(owner, Pet)` | serialized `{"$type":"Pet, ...","Value":{"$type":"Cat..."}}`, **deserialized with `Value == null`, no exception** |
| Akka.Serialization.V2 (1.6.0-beta5), `PaymentRecorded(string, PaymentMethod)` where `union PaymentMethod(Card, Invoice)` | round-trips correctly (equality true) |
| System.Text.Json on .NET 11 RC3 | serializes the case untagged (`"Pet":{"Name":"Tom"}`); deserialization throws "ambiguous for union type ... Specify a custom type classifier". STJ has new `JsonUnionAttribute`/`JsonTypeClassifier` APIs (UNVERIFIED usage; the C# research notes should cover them) |
| `union.ToString()` | prints just the type name (`PaymentMethod`), so log `.Value` instead |

Book guidance:
- **Messages and persisted events are concrete sealed records**, with marker interfaces such as `IOrderEvent` for handler and serializer binding.
- **Unions model closed sets inside the domain:**
  - decide results (`Decision`)
  - the event set the aggregate applies (`OrderEvent`, with exhaustive `Apply`)
  - value objects with variants
- Bridge between the two with `e.Value` going out and a small `ToUnion(IOrderEvent)` coming in. Alternatively, use `Recover<OrderPlaced>(e => Apply(e))` per case and let the implicit case-to-union conversion do the work.
- **Never put a union in a message or event with the default 1.5 serializer.** If you must, use 1.6 Serialization.V2 or a custom serializer.

---

## 6. Akka.Hosting (1.5.73): exact builder APIs

Docs: https://getakka.net/articles/hosting/index.html. The README is at https://github.com/akkadotnet/Akka.Hosting; that repo is to be archived after the move into the main repo.

### Core (`Akka.Hosting`)

```
IServiceCollection.AddAkka(string actorSystemName, Action<AkkaConfigurationBuilder> builder)
IServiceCollection.AddAkka(string actorSystemName, Action<AkkaConfigurationBuilder, IServiceProvider> builder)
WithActors(Action<ActorSystem, IActorRegistry>)
WithActors(Action<ActorSystem, IActorRegistry, IDependencyResolver>)
WithActors(ActorStarter)                // async: Task (ActorSystem, IActorRegistry)
WithActors(ActorStarterWithResolver)    // async: Task (ActorSystem, IActorRegistry, IDependencyResolver)
AddStartup(Action<ActorSystem, IActorRegistry>)   // runs after actors start
AddHocon(Config|IConfiguration, HoconAddMode)  AddHoconFile(path, HoconAddMode)  AddSetup(Setup)
WithActorAskTimeout(TimeSpan)  WithStrictSerialization(bool = true)  WithActorRefProvider(ProviderSelection)
WithCustomSerializer(string serializerIdentifier, IEnumerable<Type> boundTypes, Func<ExtendedActorSystem, Serializer> factory)
WithExtension<T>() / WithExtensions(params Type[])
ConfigureLoggers(Action<LoggerConfigBuilder>)  -> ClearLoggers(), AddLogger<T>(), AddLoggerFactory([ILoggerFactory]),
                                                  AddDefaultLogger(), WithDefaultLogMessageFormatter<T>(), WithLogFilter(...)
WithHealthCheck(name, IAkkaHealthCheck | Func<ActorSystem, ActorRegistry, CancellationToken, Task<HealthCheckResult>>, failureStatus?, tags?, timeout?)
WithActorSystemLivenessCheck(failureStatus?, tags?)
OpenTelemetryLoggerOptions.AddAkkaTraceCorrelation()  // Akka.Hosting.Logging.AkkaTraceContextProcessor
```

### ActorRegistry and `IRequiredActor<TKey>`

- `ActorRegistry.For(system)`, with `Register<TKey>(IActorRef, overwrite=false)`, `TryRegister<TKey>`, `Get<TKey>()`, `TryGet<TKey>(out)` and `GetAsync<TKey>(ct)`.
- `IRequiredActor<TKey>` exposes `IActorRef ActorRef { get; }` **and** `Task<IActorRef> GetAsync(CancellationToken = default)`. Prefer `GetAsync` outside actors, because it waits for registration.
- `ActorSystem` and `ActorRegistry` are registered in DI automatically.
- `TKey` is just a marker type. It can be the actor class or an empty `sealed class OrderRegion;` as in our sample.
- DI into actors goes through `IDependencyResolver`: `resolver.Props<TActor>(args...)`. Actor lifetime is unbounded, so injected scoped services need care. The Hosting README says so explicitly.

### Remote and Cluster (`Akka.Remote.Hosting`, `Akka.Cluster.Hosting`)

```
WithRemoting(string hostname = null, int? port = null, string publicHostname = null, int? publicPort = null)
WithRemoting(Action<RemoteOptions>)    // HostName, Port, PublicHostName, PublicPort, EnableSsl, Ssl, MaxFrameSize, failure detectors...
WithClustering(ClusterOptions options = null)
   ClusterOptions: Roles, SeedNodes, MinimumNumberOfMembers, MinimumNumberOfMembersPerRole, AppVersion,
                   SplitBrainResolver (KeepMajorityOption | StaticQuorumOption | KeepOldestOption | LeaseMajorityOption),
                   FailureDetector, LogInfo, LogInfoVerbose, ClusterReadyCheck
WithAkkaClusterReadinessCheck(failureStatus?, tags?)
WithShardRegion<TKey>(string typeName, Func<string, Props> entityPropsFactory, IMessageExtractor, ShardOptions)
WithShardRegion<TKey>(string typeName, Func<ActorSystem, IActorRegistry, IDependencyResolver, Func<string, Props>>, IMessageExtractor, ShardOptions)
   (+ overloads taking ExtractEntityId/ExtractShardId delegates, and a (system, registry) factory)
WithShardRegionProxy<TKey>(typeName, roleName, IMessageExtractor)
WithShardedDaemonProcess<TKey>(name, numberOfInstances, Func<ActorSystem, IActorRegistry, IDependencyResolver, Func<int, Props>>, ClusterDaemonOptions?)
WithSingleton<TKey>(singletonName, Props | factory, ClusterSingletonOptions? = null, bool createProxyToo = true)
WithSingletonProxy<TKey>(...)  WithDistributedPubSub(role)  WithDistributedData(...)  WithShardingDistributedData(...)
WithClusterClientReceptionist(...)  WithClusterClient<TKey>(...)  WithClusterClientDiscovery<TKey>(...)
   ShardOptions: Role, StateStoreMode (Persistence|DData|Custom), RememberEntities, RememberEntitiesStore (DData|Eventsourced|Custom),
                 JournalPluginId/JournalOptions, SnapshotPluginId/SnapshotOptions, LeaseImplementation, LeaseRetryInterval,
                 HandOffStopMessage, ShouldPassivateIdleEntities, PassivateIdleEntityAfter, ShardRegionQueryTimeout,
                 FailOnInvalidEntityStateTransition, DistributedData
```

### Persistence (`Akka.Persistence.Hosting`, `Akka.Persistence.Sql.Hosting`)

```
WithInMemoryJournal()  WithInMemoryJournal(Action<AkkaPersistenceJournalBuilder>, string journalId = "inmem", bool isDefaultPlugin = true)
WithInMemorySnapshotStore(string snapshotStoreId = "inmem", bool isDefaultPlugin = true)
WithJournal(JournalOptions[, Action<AkkaPersistenceJournalBuilder>])  WithSnapshot(SnapshotOptions[, ...])  WithJournalAndSnapshot(...)
WithClusterShardingJournalMigrationAdapter(...)

WithSqlPersistence(string connectionString, string providerName, PersistenceMode mode = Both, string schemaName = null,
                   Action<AkkaPersistenceJournalBuilder> journalBuilder = null, Action<AkkaPersistenceSnapshotBuilder> snapshotBuilder = null,
                   bool autoInitialize = true, string pluginIdentifier = "sql", bool isDefaultPlugin = true,
                   DatabaseMapping? databaseMapping = null, TagMode? tagStorageMode = null, bool? deleteCompatibilityMode = null,
                   bool? useWriterUuidColumn = null, int? maxConcurrentQueries = null)
WithSqlPersistence(DataOptions dataOptions, ...)                             // e.g. NpgsqlDataSource-backed
WithSqlPersistence(SqlJournalOptions? journalOptions, SqlSnapshotOptions? snapshotOptions, journalBuilder?, snapshotBuilder?)
WithSqlPersistence(Action<SqlJournalOptions>?, Action<SqlSnapshotOptions>?, bool isDefaultPlugin = true)
   enums: PersistenceMode {Both, Journal, SnapshotStore}; TagMode {Csv, TagTable, Both}; DatabaseMapping {Default, Sqlite, SqlServer, PostgreSql, MySql}
journalBuilder.WithConnectivityCheck(...) / snapshotBuilder.WithConnectivityCheck(...)   // Sql-specific health checks
```

Provider names are `LinqToDB.ProviderName` constants, for example `ProviderName.SQLiteMS` (verified), `ProviderName.PostgreSQL` and `ProviderName.SqlServer2019`. The README advises using the most specific provider name. `DatabaseMapping` and `deleteCompatibilityMode` matter only when migrating from the legacy Sql.Common schemas.

### Verified full host (single-node cluster + sharding + SQL persistence + DI registry + coordinated-shutdown task)

```csharp
builder.Services.AddAkka("shop", (akka, sp) =>
{
    akka
        .ConfigureLoggers(l => l.AddLoggerFactory())
        .WithRemoting(hostname: "localhost", port: 8110)
        .WithClustering(new ClusterOptions
        {
            Roles = ["orders"],
            SeedNodes = ["akka.tcp://shop@localhost:8110"],
        })
        .WithSqlPersistence(
            connectionString: connStr,
            providerName: ProviderName.SQLiteMS,
            mode: PersistenceMode.Both,
            autoInitialize: true,
            journalBuilder: j => j.AddWriteEventAdapter<OrderTaggingAdapter>("order-tagger", [typeof(IOrderEvent)]))
        .WithShardRegion<OrderRegion>(
            typeName: "order",
            entityPropsFactory: entityId => Props.Create(() => new OrderActor(entityId)),
            messageExtractor: new OrderMessageExtractor(maxShards: 50),
            shardOptions: new ShardOptions
            {
                Role = "orders",
                StateStoreMode = StateStoreMode.DData,
                RememberEntities = false,
                PassivateIdleEntityAfter = TimeSpan.FromMinutes(2),
            })
        .WithActors((system, registry, resolver) =>
        {
            var cache = system.ActorOf(BackoffExample.Wrap(Props.Create(() => new PriceCacheActor())), "price-cache-supervisor");
            registry.Register<PriceCacheActor>(cache);
        })
        .AddStartup((system, registry) =>
        {
            CoordinatedShutdown.Get(system).AddTask(CoordinatedShutdown.PhaseBeforeServiceUnbind, "log-goodbye", () =>
            {
                system.Log.Info("before-service-unbind: draining");
                return Task.FromResult(Done.Instance);
            });
        });
});
// later:
var region = await host.Services.GetRequiredService<IRequiredActor<OrderRegion>>().GetAsync();
var reply  = await region.Ask<object>(new PlaceOrder("o-1", "c-42"), TimeSpan.FromSeconds(10));
```

Required usings:
- `Akka`, `Akka.Actor`, `Akka.Event` (for `system.Log.Info`), `Akka.Hosting`
- `Akka.Remote.Hosting`, `Akka.Cluster.Hosting`, `Akka.Cluster.Sharding`
- `Akka.Persistence.Hosting`, `Akka.Persistence.Sql.Hosting`, `LinqToDB`

Note: with Hosting, the provider is set through a `Setup`, not HOCON. The config dump showed `akka.actor.provider = LocalActorRefProvider` even though clustering was active. Don't teach readers to read `akka.actor.provider` to detect clustering.

---

## 7. Cluster Sharding details

### Message extractor [VERIFIED]

```csharp
public sealed class OrderMessageExtractor(int maxShards) : HashCodeMessageExtractor(maxShards)
{
    public override string EntityId(object message) => message switch
    {
        IOrderCommand c => c.OrderId,
        _ => null!
    };
}
```

- `HashCodeMessageExtractor.Create(int maxNumberOfShards, Func<object,string> entityIdExtractor, Func<object,object> messageExtractor)` is the non-subclass form.
- `ShardRegion.StartEntity` and `ShardingEnvelope` are auto-handled since 1.5.15. Handling them yourself trips AK2001.
- **Shard count:** the rule of thumb is about 10x the maximum node count, and it **must never change for the lifetime of the cluster** (docs).

### Passivation

- The verified default is `passivate-idle-entity-after = 120s`, set via `ShardOptions.PassivateIdleEntityAfter` / `ShouldPassivateIdleEntities`.
- Manual passivation: `Context.Parent.Tell(new Passivate(stopMessage))`. Don't call `Context.Stop(Self)` on entities (docs warning). `HandOffStopMessage` defaults to `PoisonPill`. Use a custom stop message if the entity needs to flush.
- Passivation is disabled when remember-entities is on.

### Remember entities and state store

Verified effective defaults:

```
akka.cluster.sharding.state-store-mode = persistence
akka.cluster.sharding.remember-entities = off
akka.cluster.sharding.remember-entities-store = ddata
```

Docs say to "move to using `state-store-mode=ddata` for all new and existing applications", because `persistence` mode is being deprecated after 1.5. Switching requires a full cluster restart.

ddata durable keys default to `[shard-*]`, which uses LMDB on disk. In containers you may want `distributed-data.durable.keys = []` (UNVERIFIED best practice; docs mention the setting).

### Rebalancing and SBR

- **Allocation:** `LeastShardAllocationStrategy` is the default. 1.5 defaults are `rebalance-absolute-limit = 0` and `rebalance-relative-limit = 0.1`. 1.6 changes the absolute limit to 20.
- **SBR** (verified effective config):
  - `downing-provider-class = "Akka.Cluster.SBR.SplitBrainResolverProvider, Akka.Cluster"`
  - `active-strategy = keep-majority`
  - `stable-after = 20s`
  - `down-all-when-unstable = on`
- **Alternatives:** `static-quorum`, `keep-oldest`, `down-all`, `lease-majority` (with `Akka.Coordination.KubernetesApi` lease). Source: https://getakka.net/articles/clustering/split-brain-resolver.html. In Hosting, set them through `ClusterOptions.SplitBrainResolver = new KeepMajorityOption{...}` etc.
- `akka.cluster.run-coordinated-shutdown-when-down = on`.

---

## 8. Persistence Query and projections

### Read journal and queries [VERIFIED with Akka.Persistence.Sql + SQLite]

```csharp
var readJournal = PersistenceQuery.Get(system).ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);  // Akka.Persistence.Sql.Query
var events = await readJournal
    .CurrentEventsByTag("order", Offset.NoOffset())
    .Select(env => $"{env.PersistenceId}#{env.SequenceNr} offset={((Sequence)env.Offset).Value} {env.Event.GetType().Name}")
    .RunWith(Sink.Seq<string>(), system.Materializer());
var byPid = await readJournal.CurrentEventsByPersistenceId("order-o-1", 0, long.MaxValue)
    .RunWith(Sink.Seq<EventEnvelope>(), system.Materializer());
```

Output:

```
order-o-1#1 offset=1 OrderPlaced
order-o-1#2 offset=2 ItemAdded
order-o-1#3 offset=3 ItemAdded
order-o-1#4 offset=4 OrderSubmitted
```

**Offset semantics (verified):** `CurrentEventsByTag("order", Offset.Sequence(2))` returned offsets `[3,4]`. **The offset is exclusive.** Store the last processed offset and resume from it.

Standard query interfaces in `Akka.Persistence.Query`:
- `IEventsByTagQuery`/`ICurrentEventsByTagQuery`
- `IEventsByPersistenceIdQuery`/`ICurrentEventsByPersistenceIdQuery`
- `IPersistenceIdsQuery`/`ICurrentPersistenceIdsQuery`
- `IAllEventsQuery`/`ICurrentAllEventsQuery`

Offset types are `Sequence`, `TimeBasedUuid`, `NoOffset` and `FromEnd`. Ordering across persistence IDs is journal-specific. Akka.Persistence.Sql uses the global `ordering` column.

### Resumable projection with restart/backoff [VERIFIED, saw 4 events then picked up the 5th live]

```csharp
public static (UniqueKillSwitch, Task) Run(ActorSystem system, IOffsetStore offsets, Action<IOrderEvent> apply)
{
    var readJournal = PersistenceQuery.Get(system).ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);

    var restarting = RestartSource.OnFailuresWithBackoff(
        () => Source.FromTask(offsets.LoadAsync(Name))
            .ConcatMany(last => readJournal.EventsByTag("order", Offset.Sequence(last)))
            .SelectAsync(1, async env =>
            {
                if (env.Event is IOrderEvent e) apply(e);           // idempotent upsert in real code
                await offsets.SaveAsync(Name, ((Sequence)env.Offset).Value);
                return NotUsed.Instance;
            }),
        RestartSettings.Create(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), 0.2));

    var (killSwitch, done) = restarting
        .ViaMaterialized(KillSwitches.Single<NotUsed>(), Keep.Right)
        .ToMaterialized(Sink.Ignore<NotUsed>(), Keep.Both)
        .Run(system.Materializer());
    return (killSwitch, done);
}
```

Delivery is at-least-once: offsets are saved after apply, so projection writes must be idempotent. For production, run the projection as a `WithSingleton` or a `WithShardedDaemonProcess` (one per tag partition) so that exactly one node runs it.

**Latency tuning (Akka.Persistence.Sql README).** Default polling gives 500–1200 ms projection latency. Tune *both* settings below; tuning only `refresh-interval` doesn't work.

```hocon
akka.persistence.query.journal.sql {
  refresh-interval = 50ms
  journal-sequence-retrieval.query-delay = 50ms
}
```

Use 10 ms/10 ms for low latency, at the cost of higher DB load.

Akka.Streams note: 1.6 makes `Dispose()` on the shared `system.Materializer()` a no-op. In 1.5 **don't dispose the shared materializer**: a `using var mat = system.Materializer()` breaks every later stream.

---

## 9. Serialization (what Akka recommends now)

**Verified effective 1.5.73 defaults:**
- `System.Object : json` (Newtonsoft)
- `System.Byte[] : bytes`
- `Google.Protobuf.IMessage : proto`
- internal Akka types on `akka-misc`, `primitive`, `akka-cluster`, `akka-sharding` and similar
- `akka.actor.serialization-settings.allow-unregistered-types = true`

What lands in the journal by default (observed SQLite row):

```
{"$id":"1","$type":"Shop.Domain.OrderPlaced, Verify","OrderId":"o-1","CustomerId":"c-42"}   identifier=1, manifest=''
```

The CLR type name **and assembly name** are baked into every event. Renaming the type, its namespace or its assembly breaks recovery. This is the central motivating example for the event-versioning chapter.

**Docs position** (https://getakka.net/articles/serialization/serialization.html):
- `SerializerWithStringManifest` is recommended for evolving data and "especially useful for Persistence".
- "Use schema-based serialization (contract-bound) — more setup upfront, but faster and fundamentally more secure."
- `allow-unregistered-types = false` (1.5.66+) can block the reflection fallback.
- IDs 0–40 (docs) are reserved; pick custom IDs in 100–1000 (docs example). 1.6 V2 docs say "ids 1-100 are reserved" and use 120001 in examples.
- **Stale doc sentence:** "Starting from Akka.NET v1.5, default Newtonsoft.Json serializer will be replaced in favor of Hyperion." That never happened (verified: `System.Object : json`).
- **Hyperion** is "NOT designed as a safe serializer to be used in an open network". The library has been quiet since May 2025, at 0.12.2. Don't recommend it for new code.

**Recommended approach for the book (1.5.73):** a System.Text.Json `SerializerWithStringManifest` with explicit, versioned manifests. **[VERIFIED round-trip]:**

```csharp
public sealed class OrderEventSerializer(ExtendedActorSystem system) : SerializerWithStringManifest(system)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private static readonly FrozenDictionary<Type, string> ToManifest = new Dictionary<Type, string>
    {
        [typeof(OrderPlaced)] = "order-placed.v1",
        [typeof(OrderSubmitted)] = "order-submitted.v1",
    }.ToFrozenDictionary();
    private static readonly FrozenDictionary<string, Type> FromManifest =
        ToManifest.ToFrozenDictionary(kv => kv.Value, kv => kv.Key);

    public override int Identifier => 500;
    public override string Manifest(object o) => ToManifest.TryGetValue(o.GetType(), out var m)
        ? m : throw new ArgumentException($"No manifest for {o.GetType()}");
    public override byte[] ToBinary(object obj) => JsonSerializer.SerializeToUtf8Bytes(obj, obj.GetType(), Options);
    public override object FromBinary(byte[] bytes, string manifest) => FromManifest.TryGetValue(manifest, out var t)
        ? JsonSerializer.Deserialize(bytes, t, Options)!
        : throw new System.Runtime.Serialization.SerializationException($"Unknown manifest {manifest}");
}
// Hosting: akka.WithCustomSerializer("order-json", [typeof(IOrderEvent)], sys => new OrderEventSerializer(sys));
// -> FindSerializerFor(new OrderPlaced(..)) == OrderEventSerializer id=500 manifest=order-placed.v1 json={"orderId":"o-1","customerId":"c-1"}
```

Upcasting then happens in `FromBinary` (switch on `"order-placed.v1"` vs `".v2"`) or in an `IReadEventAdapter`. Protobuf (`Google.Protobuf`, already bound to `proto`) is the other schema-first option; it is UNVERIFIED in this spike.

**1.6 forward path [VERIFIED on 1.6.0-beta5]:**

```csharp
[AkkaSerializable(Manifest = "payment-recorded-v1")]
public sealed record PaymentRecorded(
    [property: AkkaField(0)] string OrderId,
    [property: AkkaField(1)] PaymentMethod Method) : IOrderEvent;   // PaymentMethod is a C# union of two [AkkaSerializable] records

[AkkaSerializer<IOrderEvent>("order-events", 120001)]
public sealed partial class OrderSerializer : AkkaSerializer
{
    public static partial global::Akka.Serialization.SerializerDetails CreateSerializerDetails(global::Akka.Actor.ExtendedActorSystem system);
}
// akka.WithCustomSerializer(OrderSerializer.CreateSerializerDetails);   // 1.6 Hosting overload
```

Output: `OrderSerializer#120001 manifest='payment-recorded-v1' 32B roundtrip-equal=True`.

**Hardening to recommend:**
- `WithStrictSerialization()` (Hosting) or `serialize-messages = on` in tests catches unserializable messages early. UNVERIFIED: exact HOCON that `WithStrictSerialization` sets.
- Set `allow-unregistered-types = false` once all bindings exist.

---

## 10. Coordinated shutdown [VERIFIED effective config]

Phases, in order (timeout `5 s` unless noted):

```
before-service-unbind -> service-unbind -> service-requests-done -> service-stop -> before-cluster-shutdown
-> cluster-sharding-shutdown-region (10 s) -> cluster-leave -> cluster-exiting (10 s) -> cluster-exiting-done
-> cluster-shutdown -> before-actor-system-terminate -> actor-system-terminate (10 s)
```

Other defaults:
- `run-by-clr-shutdown-hook = on`, `exit-clr = off`, `default-phase-timeout = 5 s`.
- The docs note that SIGTERM triggers the CLR hook, but SIGINT doesn't.
- With Akka.Hosting, `host.StopAsync()` ran CoordinatedShutdown. The log showed "CoordinatedShutdown invoked due to [ClrExitReason]", followed by our `before-service-unbind` task, shard hand-off and leaving the cluster.

API:

```csharp
CoordinatedShutdown.Get(system).AddTask(CoordinatedShutdown.PhaseBeforeServiceUnbind, "name", () => Task.FromResult(Done.Instance));
```

Other phase constants follow the same pattern, e.g. `PhaseClusterLeave`. Cluster, sharding hand-off and singleton hand-off tasks are registered automatically. Register your own tasks at startup (`AddStartup`).

Kubernetes (Petabridge guide, https://petabridge.com/blog/akkadotnet-guide-to-kubernetes/):
- Set `terminationGracePeriodSeconds` to about 35 s or more.
- Prefer **Deployments over StatefulSets**.
- Use RollingUpdate with `maxSurge: 1` and `maxUnavailable: 0`.
- Use a headless Service with `publishNotReadyAddresses: true`.
- Add `WithAkkaClusterReadinessCheck()` and `WithActorSystemLivenessCheck()` mapped to `/healthz/ready` and `/healthz/live`.

---

## 11. Akka.Management, discovery and Aspire (signatures from 1.5.73 reflection)

```
WithAkkaManagement(string hostName = null, int? port = null, string bindHostname = null, int? bindPort = null, bool autoStart = false)
WithAkkaManagement(Action<AkkaManagementOptions> configure, bool autoStart = false)          // also AkkaManagementSetup overloads
WithClusterBootstrap(string serviceName = null, string serviceNamespace = null, string portName = null,
                     int? requiredContactPoints = null, bool? newClusterEnabled = null, bool autoStart = true, ...)
WithClusterBootstrap(Action<ClusterBootstrapOptions> configure, bool autoStart = true)        // in package Akka.Management
WithKubernetesDiscovery(string podLabelSelector = null)
WithKubernetesDiscovery(Action<KubernetesDiscoveryOptions>)   // PodLabelSelector, PodNamespace, AllNamespaces, RawIp, ContainerName, ...
```

Pattern:
- Clear static `SeedNodes` and let Bootstrap form the cluster.
- Leave the management hostname blank so it binds to the pod IP.
- Discovery selects pods by label `app={ServiceName}` by default (Petabridge K8s guide).
- Lease-based SBR uses `Akka.Coordination.KubernetesApi` together with `LeaseMajorityOption`. The exact Hosting wiring is UNVERIFIED and left for the ops research.

Aspire (https://petabridge.com/blog/akkadotnet-aspire/, Sep 2026; packages `Akka.Aspire.Hosting` and `Akka.Aspire` 1.5.73):
- In the AppHost: `builder.AddAkka("sample-cluster").WithClustering(redis)`, then `.WithReference(akka)`.
- In the service: `akkaBuilder.WithAspireClusterBootstrap(sp, configureDiscovery: (b, config) => b.WithRedisDiscovery(...), clusterConfigure: c => c.Roles = [...])`.
- These are UNVERIFIED compiles; good for the "local dev cluster" section.

---

## 12. Testing (brief; the testing research covers depth) [VERIFIED]

- **`Akka.Hosting.TestKit` 1.5.73 is xunit v3** (depends on `Akka.TestKit.Xunit` and `xunit.v3.extensibility.core` 3.2.2). Use `Akka.TestKit.Xunit2` for xunit v2.
- 2 tests passed under `dotnet test` on net11.0 with `xunit.v3` 3.2.2, `xunit.runner.visualstudio` 3.1.5 and `Microsoft.NET.Test.Sdk` 18.11.0. The xUnit1051 analyzer warns to pass `TestContext.Current.CancellationToken`.

```csharp
public class OrderActorSpecs(ITestOutputHelper output) : Akka.Hosting.TestKit.TestKit(nameof(OrderActorSpecs), output)
{
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
        => builder.WithInMemoryJournal().WithInMemorySnapshotStore();

    [Fact]
    public async Task Order_survives_restart()
    {
        var order = Sys.ActorOf(Props.Create(() => new OrderActor("o-9")));
        order.Tell(new PlaceOrder("o-9", "c-1"), TestActor);
        await ExpectMsgAsync<CommandAccepted>();
        order.Tell(new AddItem("o-9", "sku", 2, 3m), TestActor);
        await ExpectMsgAsync<CommandAccepted>();
        Watch(order); Sys.Stop(order); await ExpectTerminatedAsync(order);
        var revived = Sys.ActorOf(Props.Create(() => new OrderActor("o-9")));
        revived.Tell(new GetOrder("o-9"), TestActor);
        Assert.Equal(6m, (await ExpectMsgAsync<OrderView>()).Total);
    }
}
```

- Pure domain tests (`OrderState.Decide`/`Apply`) need no actors at all. This is the "functional core" testing story.
- `Akka.Hosting.TestKit` also exposes the Persistence TestKit fault-injection hooks: `WithJournalWrite`, `WithJournalRecovery`, `WithSnapshotSave`, `WithSnapshotLoad` and `WithSnapshotDelete`.
- Petabridge removed FluentAssertions from Akka.NET (blog 2026-06-10), so use plain xUnit asserts.

---

## 13. Gotchas checklist for writers

1. **Pin all `Akka.*` packages to one version** (1.5.73). Hosting is lock-step now; mixing a 1.5.6x Hosting with 1.5.73 core is wrong.
2. **Don't reference `Akka.Persistence.PostgreSql`, `.SqlServer` or `.Sqlite`** (deprecated). Use `Akka.Persistence.Sql.Hosting` with a `LinqToDB.ProviderName.*` constant.
3. **Unions:**
   - Never `Tell` or `Persist` a union struct. Send or persist `.Value` and the case records.
   - `Receive<Case>` won't match a boxed union.
   - Newtonsoft silently nulls union values.
   - Handle the `null` arm or accept CS8655.
   - Log `.Value`, not the union.
4. **Always `Ask` with a timeout.** The default is infinite.
5. **Register `Command<GetX>` before `Command<IBaseCommand>`.** First match wins.
6. **Set `StateStoreMode = DData` explicitly.** The default is still `persistence`.
7. **Don't map `StartEntity`/`ShardingEnvelope` in extractors** (AK2001). Never change `maxNumberOfShards` on a live cluster.
8. **Use `PersistAll` rather than `Persist` in a loop** (AK1006). Capture `Sender` before persisting if you reply in `onComplete`, as in the sample.
9. **Persistence query offsets are exclusive** in Akka.Persistence.Sql. Projections are at-least-once.
10. **The default JSON serializer embeds assembly-qualified type names.** Introduce a manifest-based serializer before the first persisted event in the sample.
11. **Don't dispose `system.Materializer()` in 1.5.**
12. **`ReceiveAsync` blocks the mailbox until done.** Use `PipeTo` plus stash for long I/O.
13. **Don't read the provider from HOCON when using Hosting**; the provider is set via Setup.
14. **Health checks are built into Akka.Hosting.** Don't add `Akka.HealthCheck.*` (stale 1.5.37).
15. **Test projects:** Akka.Hosting.TestKit requires **xunit v3**. 1.5.72+ TestKit requires `override` on `InitializeAsync`/`DisposeAsync` if you declare them.
16. **`akka.cluster.app-version` defaults to `assembly-version`.** Set `ClusterOptions.AppVersion` deliberately for rolling updates. This is a hook for the releases and versioning chapter.

---

## 14. Sources

- NuGet flat-container / registration APIs: `https://api.nuget.org/v3-flatcontainer/<id>/index.json`, `https://api.nuget.org/v3/registration5-gz-semver2/<id>/index.json` (queried 2026-11-04)
- Akka.NET releases: https://github.com/akkadotnet/akka.net/releases (1.5.72, 1.5.73, 1.6.0-beta1…beta5 notes)
- https://github.com/akkadotnet/akka.net/blob/dev/GETTING_STARTED_V1.6.md
- https://github.com/akkadotnet/akka.net/blob/dev/BREAKING_CHANGES_V1.6.md
- Milestones: https://github.com/akkadotnet/akka.net/milestones (1.6.0: no due date, 53 open / 90 closed)
- Akka.Hosting README: https://github.com/akkadotnet/Akka.Hosting (DI, IRequiredActor, OTel trace correlation, health checks); new docs home https://getakka.net/articles/hosting/index.html
- Akka.Persistence.Sql README: https://github.com/akkadotnet/Akka.Persistence.Sql (Hosting usage, DataOptions, providers, compat modes, query latency tuning)
- Serialization: https://getakka.net/articles/serialization/serialization.html
- Split brain resolver: https://getakka.net/articles/clustering/split-brain-resolver.html
- Cluster sharding: https://getakka.net/articles/clustering/cluster-sharding.html
- Coordinated shutdown: https://getakka.net/articles/actors/coordinated-shutdown.html
- Persistence Query: https://getakka.net/articles/persistence/persistence-query.html
- Receive actor API: https://getakka.net/articles/actors/receive-actor-api.html
- Analyzers: https://getakka.net/articles/debugging/akka-analyzers.html
- Petabridge blog:
  - v1.6 roadmap: https://petabridge.com/blog/akkadotnet-v1.6-roadmap/
  - Retiring Sql.Common plugins: https://petabridge.com/blog/retiring-akka-persistence-sql-common/
  - Kubernetes guide: https://petabridge.com/blog/akkadotnet-guide-to-kubernetes/
  - Aspire: https://petabridge.com/blog/akkadotnet-aspire/
  - Health checks: https://petabridge.com/blog/you-dont-need-akka-healthchecks-anymore/
  - CRUD to CQRS: https://petabridge.com/blog/crud-to-cqrs-akka-persistence/
  - Streams OTel tracing: https://petabridge.com/blog/akkadotnet-streams-opentelemetry-tracing/ (not read; for the telemetry research)
  - Long-running tasks: https://petabridge.com/blog/akkadotnet-long-running-tasks/ (not read)
- Verified spikes: `/home/jonathan/code/ddd-actors-dotnet/research/akka-core-verify/` (README lists each project)
