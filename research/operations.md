# Research notes: Operations, releases & versioning for an event-sourced Akka.NET cluster on .NET 11

Researched 2026-11-04. Toolchain: .NET SDK `11.0.100-rc.3.26522.148` (via `tools/env.sh`), podman, Aspire CLI 13.6.3.
Verified code lives in `research/operations-snippets/`:

- `Ops.Verify`: console app covering event adapters, the serializer, the union gotcha, MinVer and AppVersion.
- `Ops.Node`: Kubernetes-ready web node.
- `Ops.AppHost`: Aspire.
- `k8s/` and `.github/`: YAML.

Anything marked **VERIFIED** was compiled and run on this machine against the stated package versions. Anything marked **UNVERIFIED** was not.

---

## 0. TL;DR for the chapter author

1. **Stay on Akka.NET 1.5.73 (stable) for the book's sample; treat 1.6 as a "what's coming" sidebar.**
   - 1.6.0-beta5 shipped today (2026-11-04).
   - 1.6 targets `net10.0` only. It changes default serializer bindings, which is a Wire/persistence-visible change. It changes the default sharding rebalance limits.
   - 1.6 adds `Akka.Serialization.V2`, a MessagePack source generator. As of beta5 it supports C# 15 unions as field types.
2. **The `app-version = assembly-version` default silently breaks with MinVer.**
   - MinVer sets `AssemblyVersion` to `{Major}.0.0.0`, so every 1.x build reports AppVersion `1.0.0`.
   - Sharding's "allocate to the newest version" and "don't rebalance during a rolling update" logic then sees no difference between old and new nodes. **VERIFIED.**
3. **Akka's `AppVersion` parser throws on SemVer 2 dotted pre-release identifiers.** Examples: `1.4.3-alpha.0.1` (MinVer height builds) and `2.0.0-rc.1`. The ActorSystem then fails to start. **VERIFIED.**
   - Feed it `MAJOR.MINOR.PATCH` only.
4. **The default persistence serializer in 1.5 is Newtonsoft JSON (id 1).** It writes `"$type":"Namespace.Type, Assembly"` into the payload with an empty manifest. **VERIFIED** by reading the SQLite journal row.
   - Renaming or moving an event type breaks replay.
   - Bind your own `SerializerWithStringManifest` with stable manifests from day one.
5. **Never `Persist()` a C# union wrapper.** With the default JSON serializer, a union round-trips to `Value == null`, so the data is silently lost. **VERIFIED.** Persist the case value.
6. **Use the SQL connectivity health check for readiness.** The standard `WithHealthCheck()` persistence check stayed **Healthy with Postgres stopped**. `WithConnectivityCheck()` went Unhealthy. **VERIFIED.**
7. **Kubernetes:** use a Deployment with `maxSurge: 1, maxUnavailable: 0`, Akka.Management + Cluster Bootstrap + Kubernetes API discovery, and a headless service with `publishNotReadyAddresses: true`. This is Petabridge's current guidance (Oct 2025).
8. **Schema:** own the SQL journal schema with migrations. Apply the published DDL and set `autoInitialize: false`. **VERIFIED** on Postgres 17.

---

## 1. Version matrix (exact, from NuGet flat-container API on 2026-11-04)

| Package / tool | Latest stable | Latest pre-release | Notes |
|---|---|---|---|
| Akka, Akka.Cluster, Akka.Cluster.Sharding, Akka.Persistence, Akka.Persistence.Query, Akka.Streams, Akka.Remote, Akka.Cluster.Tools, Akka.DependencyInjection, Akka.Persistence.TestKit | **1.5.73** | 1.6.0-beta5 | 1.5.73 libs target `net6.0` + `netstandard2.0` and run fine on net11 (VERIFIED) |
| Akka.Hosting, Akka.Cluster.Hosting, Akka.Persistence.Hosting, Akka.Hosting.TestKit | **1.5.73** | 1.6.0-beta5 | Hosting 1.5.73 requires Microsoft.Extensions ≥ 9.0. In 1.6, Hosting moved into the akka.net repo and is versioned in lock-step |
| Akka.Management (includes Cluster Bootstrap + HTTP) | **1.5.73** | 1.6.0-beta5 | `Akka.Management.Cluster.Bootstrap` as a separate package is dead (0.3.0-beta4); bootstrap lives in `Akka.Management` |
| Akka.Discovery.KubernetesApi, Akka.Coordination.KubernetesApi, Akka.Discovery.Azure, Akka.Discovery.Redis | **1.5.73** | 1.6.0-beta5 | |
| Akka.Aspire, Akka.Aspire.Hosting | **1.5.73** | 1.6.0-beta5 | New Aspire integrations in the Akka.Management repo |
| Akka.Persistence.Sql, Akka.Persistence.Sql.Hosting | **1.5.73** | 1.6.0-beta5 | 1.5.73 (2026-11-02) upgraded to LinqToDB 6.5.1 |
| Akka.Persistence.PostgreSql (legacy plugin) | 1.5.37 | — | Legacy; migrate to Akka.Persistence.Sql with `table-mapping = postgresql` |
| Akka.Persistence.Query.Sql (legacy) | 1.5.39 | — | Legacy |
| Akka.HealthCheck(.Hosting) | 1.5.37 | — | **Deprecated** in favour of health checks built into Akka.Hosting |
| Akka.Serialization.Hyperion | 1.5.73 | 1.6.0-beta5 | Like JSON, it embeds CLR type names |
| Petabridge.Cmd.Host | 1.5.2 | — | |
| Microsoft.FeatureManagement(.AspNetCore) | **4.8.0** | — | |
| MinVer | **8.0.0** | 8.1.0-alpha.1 | |
| Nerdbank.GitVersioning | **3.10.94** (2026-08-28) | 3.11.202-beta | |
| GitVersion.MsBuild / GitVersion.Tool | **6.8.2** (2026-07-10) | — | |
| Aspire.Hosting.AppHost / Aspire.AppHost.Sdk / Aspire.Hosting.PostgreSQL / Aspire.Npgsql / Aspire.Hosting.Redis | **13.6.4** | — | Aspire.Hosting.Kubernetes is 13.6.4-preview only |
| Npgsql | 10.0.3 | | |
| Microsoft.Data.Sqlite | 10.0.14 | | 10.0.0 pulls vulnerable SQLitePCLRaw 2.1.11 (NU1903 / GHSA-2m69-gcr7-jv3q); 10.0.14 does not |
| Microsoft.Extensions.Hosting | 10.0.14 | 11.0.0-rc.3 | |
| OpenTelemetry.Extensions.Hosting | 1.20.1 | 1.21.0-rc.1 | |
| Google.Protobuf | 3.36.2 | 4.0.0-rc2 | |
| FluentMigrator | 8.0.1 | | |
| dbup-postgresql / dbup-core | 7.0.1 / 6.1.1 | | |
| dotnet-ef | 10.0.14 | 11.0.0-rc.3 | |
| Linq2Db | 6.5.1 | | |

GitHub Actions, latest releases from the GitHub API:

| Action | Version | Notes |
|---|---|---|
| `actions/checkout` | v7.0.1 | |
| `actions/setup-dotnet` | v6.1.0 (2026-11-03) | node24 |
| `actions/upload-artifact` | v7.0.2 | |
| `docker/login-action` | v4.7.0 | |
| `azure/setup-helm` | v5.0.1 | |
| `GitTools/actions` | v4.7.0 | |
| `googleapis/release-please-action` | **v5.0.0** (2026-04-22) | The only breaking change is node24. The README still shows `@v4` |

---

## 2. Event & message schema evolution

### 2.1 Principles for the book

- **Events are a durable public contract.** Never delete or rename a persisted event type, manifest or field index.
  - "Deleting" an event type means keeping a read path for it, typically an upcaster, forever.
  - The journal is append-only. Rows written by v1 code will be replayed by v7 code.
- **Separate three schemas:**
  1. The *domain event* used inside the aggregate, which can be refactored freely.
  2. The *persisted representation*: serializer + manifest + bytes.
  3. The *wire message* exchanged between nodes during a rolling update.
  - Adapters and serializers are the anti-corruption layer between them.
- **Tolerant reader:**
  - Ignore unknown fields.
  - Treat missing new fields as optional, i.e. nullable or defaulted.
  - Unknown manifest means throw `SerializationException`.
- **Two-phase rollout for any new type** (Akka 1.6 docs, `source-generated-serialization.md` §Versioning):
  1. "Deploy the code that can *read* it to every node before any node *sends* it."
  2. The same applies to persisted events: deploy readers everywhere before any node *writes* the new shape.
  - In practice, release N adds the type plus the deserializer. Release N+1 starts emitting it, often behind a feature flag.

### 2.2 What actually lands in the journal (VERIFIED)

`Ops.Verify` uses Akka.Persistence.Sql 1.5.73 on SQLite (`ProviderName.SQLiteMS`) with `autoInitialize` left at its default. It persists one event with the default serializer and two with a custom serializer, then dumps the `journal` table:

```
row seq=1 serializerId=1    manifest=''                   payload={"$id":"1","$type":"Ops.Verify.OrderPlacedV1, Ops.Verify","OrderId":"order-1","Total":{"$"...
row seq=2 serializerId=9001 manifest='order-placed.v2'    payload=order-1|20|EUR
row seq=3 serializerId=9001 manifest='order-cancelled.v1' payload=order-1|
```

Takeaways:

- With the default JSON serializer (id 1) the CLR type name and assembly are *in the payload* and the manifest is empty. Renaming the namespace, the type or the assembly breaks replay.
- With a `SerializerWithStringManifest` you control the manifest. The `identifier` column stores your serializer id. **The serializer id is now a forever-contract.** Never reuse or change it.
- Postgres DDL columns, from `docs/ddl/default/postgresql/journal.sql`:
  - `ordering bigserial PK`, `deleted`, `persistence_id`, `sequence_number`, `created` (DateTime ticks), `tags`, `message bytea`, `identifier int`, `manifest text`, `writer_uuid`.
  - Unique on `(persistence_id, sequence_number)`.

### 2.3 Serializer IDs

From akka.net `docs/articles/serialization/codes.md`:

- **IDs 1–100 are reserved** for Akka.NET.
- Built-ins include:
  - 1 NewtonSoftJson
  - 2 Protobuf
  - 4 ByteArray
  - 5 Cluster
  - 7/8 Persistence message/snapshot
  - 11/12 DData
  - 13 ClusterSharding
  - 14 ClusterSingleton/PubSub
  - 16 Misc
  - 17 Primitive
  - 22 SystemMessage
  - 30 StreamRef
- Hyperion is commonly −5 (UNVERIFIED here).
- Pick a documented, app-wide range, e.g. 9000–9099, and record it in an ADR.
- In Akka 1.6, the `Akka.Serialization.V2` generator rejects duplicate IDs within a compilation (AKKASG013). It **cannot** see collisions with HOCON-registered serializers.

### 2.4 Serializer choice and its impact

| Choice | Manifest | Evolution story | Recommendation |
|---|---|---|---|
| Default Newtonsoft JSON (id 1) | empty; `$type` in payload | Renames are breaking. Field add/remove is tolerated | Prototype only; never for long-lived journals |
| Hyperion | CLR type info in payload | Same renaming problem; version-tolerance options exist | Avoid for persistence |
| Google.Protobuf + custom `SerializerWithStringManifest` | yours | Field numbers give tolerant reading; schema-first | Solid, but more ceremony |
| Hand-written `SerializerWithStringManifest` + System.Text.Json | yours, e.g. `order-placed.v2` | Upcast inside `FromBinary` by manifest | **Book default for 1.5** (simple, explicit, teachable) |
| Akka 1.6 `Akka.Serialization.V2` (MessagePack source-gen) | `[AkkaSerializable(Manifest="...")]` | Map keyed by `[AkkaField(n)]`; unknown keys skipped; **new fields must be nullable** or old rows throw "Missing required field" | Future default; beta |

Akka 1.6 docs, quoted:

> "Manifests are part of that wire contract … Changing it is exactly as breaking as changing a field index."

Akka 1.6 BREAKING_CHANGES #8702 (Behavior + **Wire**): first-party serializers are now registered by default. Concretely:

- `string`/`int`/`long` move to `primitive` (17).
- `IActorRef`, `Status.*` and similar move to `akka-misc` (16).
- Persisted rows written by 1.6 may be unreadable by 1.5 unless 1.5 loads Akka.Remote's config.
- Rollback from 1.6 to 1.5 after writing data is therefore not safe without explicit bindings.
- **Pin bindings explicitly** if you need two-way compatibility.

### 2.5 Upcasting option A: read event adapter (VERIFIED)

```csharp
public sealed record OrderPlacedV1(string OrderId, decimal Total);                // legacy, kept forever
public sealed record OrderPlaced(string OrderId, decimal Total, string Currency); // current

public sealed class OrderPlacedUpcaster : IReadEventAdapter
{
    public IEventSequence FromJournal(object evt, string manifest) => evt switch
    {
        OrderPlacedV1 v1 => EventSequence.Single(new OrderPlaced(v1.OrderId, v1.Total, "USD")),
        _ => EventSequence.Single(evt),
    };
}

// Akka.Hosting registration (Akka.Persistence.Hosting 1.5.73):
.WithSqlPersistence(conn, ProviderName.SQLiteMS,
    journalBuilder: j => j.AddReadEventAdapter<OrderPlacedUpcaster>("upcast-order-placed", [typeof(OrderPlacedV1)]));
```

On recovery the actor received `placed order-1 10 USD`: the V1 row was upcast.

Other builder methods: `AddEventAdapter<T>` (read+write) and `AddWriteEventAdapter<T>`. Adapter API (from `EventAdapters.cs`):

- `IWriteEventAdapter.Manifest(object)` and `ToJournal(object)`.
- `IReadEventAdapter.FromJournal(object, string manifest)` returning `IEventSequence`.
- Helpers: `EventSequence.Single/Create/Empty`.
- Returning `EventSequence.Empty` drops an obsolete event.
- Returning several events splits one event into many.

**Caveat:** the adapter receives an already-deserialized object. The old CLR type must therefore still be deserializable. Keep `OrderPlacedV1` in a `Legacy` namespace, but do not rename it if JSON wrote its name into the payload.

### 2.6 Upcasting option B: inside the serializer by manifest (VERIFIED)

This is preferred once you own the serializer. Old rows carry `order-placed.v1` forever, and no legacy CLR type is needed:

```csharp
public sealed class OrderEventSerializer(ExtendedActorSystem system) : SerializerWithStringManifest(system)
{
    public override int Identifier => 9001;     // forever
    public override string Manifest(object o) => o switch
    {
        OrderPlaced => "order-placed.v2",
        OrderCancelled => "order-cancelled.v1",
        _ => throw new ArgumentException($"Unknown type {o.GetType()}"),
    };
    // ToBinary elided
    public override object FromBinary(byte[] bytes, string manifest) => manifest switch
    {
        "order-placed.v1" => /* upcast: default Currency = "USD" */ ...,
        "order-placed.v2" => ...,
        "order-cancelled.v1" => ...,
        _ => throw new SerializationException($"Unknown manifest [{manifest}]"),
    };
}
// registration
.WithCustomSerializer("order-events", [typeof(OrderPlaced), typeof(OrderCancelled)], sys => new OrderEventSerializer(sys))
```

Akka docs (`serialization.md`):

> "It's recommended to throw `SerializationException` in `FromBinary` if the manifest is unknown … `SerializationException` is treated as a transient problem in the TCP based remoting layer. The problem will be logged and message is dropped. Other exceptions will tear down the TCP connection."

This is the hook that makes rolling updates survivable when a new message type reaches an old node.

### 2.7 C# 15 unions and persistence (VERIFIED, important gotcha)

`public union OrderEvent(OrderPlaced, OrderCancelled);` compiles on the RC3 SDK, and an exhaustive `switch` needs no discard arm:

```csharp
public static string Describe(OrderEvent e) => e switch
{
    OrderPlaced p    => $"placed {p.OrderId} {p.Total} {p.Currency}",
    OrderCancelled c => $"cancelled {c.OrderId} ({c.Reason ?? "no reason"})",
};
```

But serializing the **union wrapper** with Akka 1.5's default serializer produced:

```
{"$id":"1","$type":"Ops.Verify.OrderEvent, Ops.Verify","Value":{"$type":"Ops.Verify.OrderPlaced, ..."}}
roundtrip -> OrderEvent: Value=null      <-- data lost silently
```

**Rules for the book:**

- Use unions as the *in-memory* event algebra, i.e. the `Apply(OrderEvent)` / `Decide(...) : OrderEvent[]` signatures.
- Always `Persist(e.Value)`, or persist the concrete case type, and bind serializers to the case types.
- Akka 1.6.0-beta5 `Akka.Serialization.V2` adds C# 15 union support for *field types*. It detects a struct implementing `IUnion`, uses its single-parameter constructors as members, and dispatches on `Value`. That covers unions nested inside messages, not top-level persisted unions. UNVERIFIED locally.

### 2.8 Tolerant reader checklist (for a sidebar)

- Add fields as nullable or with defaults. `OrderCancelled(string OrderId, string? Reason)` reads old rows with `Reason = null`.
- Never change a field's meaning or units. Add a new field instead.
- Never reuse a manifest, field index or serializer id.
- Removing a field: stop writing it, keep reading it, and remove it only from the *domain* type.
- Splitting or merging events: use a read adapter returning `EventSequence.Create(a, b)`.
- Keep golden-file tests: a committed byte corpus per manifest version, deserialized in CI. Akka itself does this with "wire-format snapshot corpus" in 1.6.
- Snapshots are a *cache*. Version them too (see §6).

---

## 3. Wire compatibility across rolling updates

- During a rolling update, old and new nodes exchange messages: sharding envelopes containing your commands, DData, singleton and pub-sub traffic.
- Your *command/message* serializers need the same discipline as events, but only across **adjacent** versions (N−1 ↔ N).
- An unknown manifest leads to `SerializationException`, so the message is dropped, not the association. Callers must tolerate that (Ask timeouts, retries).
- **Expand/contract sequence** for a breaking message change:
  1. Release N: accept both old and new shapes.
  2. Release N+1: send the new shape.
  3. Release N+2: drop the old reader.
  - For persisted events, step 3 never happens.
- **Akka 1.5 → 1.6 is a special case.** Read every "Wire" row in `BREAKING_CHANGES_V1.6.md` before rolling.
  - Gossip `uid` grows from uint32 to uint64. It is varint-compatible while UIDs stay 32-bit. Only enable `akka.remote.use-64bit-system-uids` after all nodes run 1.6.
  - **Artery (`akka://`, port 25520) is NOT wire-compatible with classic DotNetty (`akka.tcp://`).** Switching transport requires a full cluster restart, i.e. blue/green, not rolling.
  - Artery is experimental in 1.6; do not use it in production.

---

## 4. Akka.Cluster rolling updates

### 4.1 AppVersion: how it is used (source-verified, dev branch; confirmed in 1.5.73 packaged config)

From `Cluster.conf`:

> `app-version = assembly-version` — "by default the app-version will default to the entry assembly's version". Versions can be 1–3 numeric parts with an optional qualifier, plus sbt-dynver `+N-sha` form.

`AppVersion.AppVersionFromAssemblyVersion()` uses `entryAssembly.GetName().Version`, i.e. **AssemblyVersion**, formatted as `Major.Minor.Build`.

Sharding, in `AbstractLeastShardAllocationStrategy`:

- `ShardSuitabilityOrdering`:
  1. Prefer nodes that are not leaving.
  2. Then prefer the **highest AppVersion**.
  3. Then prefer the fewest shards.
  - So new shards land on upgraded nodes.
- `IsAGoodTimeToRebalance` returns false unless **all regions have the same AppVersion**, all members are reachable and none are joining. Rebalancing is therefore paused for the duration of a rolling update. This avoids moving entities onto a node that is about to be killed.

Singleton: `akka.cluster.singleton.consider-app-version = false` by default (present in 1.5.73). When true, the singleton runs on the oldest node *with the highest app-version*, so it hops to the new version once.

Defaults differ between versions:

- 1.5.73: `least-shard-allocation-strategy.rebalance-absolute-limit = 0`, i.e. the legacy `rebalance-threshold = 1` / `max-simultaneous-rebalance = 3` mode (checked in the packaged DLL).
- 1.6: the default becomes `20` with `rebalance-relative-limit = 0.1`.

### 4.2 The MinVer / AppVersion trap (VERIFIED)

Tag `v1.4.2` with MinVer 8.0.0:

```
AssemblyVersion=1.0.0.0 InformationalVersion=1.4.2+7e4f0285...
Cluster AppVersion=1.0.0          <-- all 1.x releases look identical to Akka
```

The MinVer README documents `AssemblyVersion = {MinVerMajor}.0.0.0` and `FileVersion = {Major}.{Minor}.{Patch}.0`. NBGV defaults `assemblyVersion.precision` to `major.minor`, so you get `1.4.0` and patch releases are invisible. GitVersion's built-in workflows use `assembly-versioning-scheme: MajorMinorPatch`, which works out of the box.

The AppVersion parser was tested directly (VERIFIED):

| Input | Result |
|---|---|
| `1.4.2`, `1.10.0`, `1.4.3-alpha`, `1.4.3-rc1`, `1.4.3-preview-0001`, `1.4.2+5-abc123` | OK |
| `1.4.3-alpha.0.1` (MinVer height), `2.0.0-rc.1`, `1.4.2+5.sha.abc` | **throws `ArgumentOutOfRangeException`** ("Only 3 digits separated with '.' are supported") |
| Ordering | `1.10.0 > 1.4.3` (numeric), `1.4.3-rc1 < 1.4.3`, `1.4.2+5-abc123 > 1.4.2` |

An unparseable `Akka:AppVersion` makes the ActorSystem fail at startup. **Fix, either one (both VERIFIED):**

```xml
<!-- (a) Services only: make assembly-version track MAJOR.MINOR.PATCH under MinVer -->
<Target Name="AkkaAppVersionFromMinVer" AfterTargets="MinVer">
  <PropertyGroup>
    <AssemblyVersion>$(MinVerMajor).$(MinVerMinor).$(MinVerPatch).0</AssemblyVersion>
  </PropertyGroup>
</Target>
```

This produced `AssemblyVersion=1.4.3.0`, InformationalVersion `1.4.3-alpha.0.1+b6f4…`, and `Cluster AppVersion=1.4.3`.

```csharp
// (b) Explicit, from config/env (Akka__AppVersion), falling back to InformationalVersion stripped of pre-release/metadata
var appVersion = cfg["Akka:AppVersion"]
    ?? typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
           .InformationalVersion.Split('+', '-')[0];
akka.WithClustering(new ClusterOptions { AppVersion = appVersion, ... });
```

Do not change AssemblyVersion on every patch for *libraries* you publish to NuGet. It causes binding-redirect pain on .NET Framework and strong-name churn. This is a service-only trick.

### 4.3 Coordinated shutdown and process lifecycle

The sequence on SIGTERM is:

1. Kubernetes sends SIGTERM.
2. The .NET host begins `StopAsync`.
3. Akka.Hosting's `AkkaHostedService.StopAsync` runs `CoordinatedShutdown.Run(ClrExitReason.Instance)`.
4. Coordinated shutdown proceeds through these phases:
   - `cluster-sharding-shutdown-region` hands off shards.
   - `cluster-leave` / `cluster-exiting` run next.
   - The ActorSystem then terminates.

If the cluster downs the node instead (`ClusterDowningReason` / `ClusterLeavingReason`), Akka.Hosting calls `IHostApplicationLifetime.StopApplication()` so the process exits and Kubernetes restarts it. Source: `AkkaHostedService.cs`.

Timing budget:

- `terminationGracePeriodSeconds` (Petabridge example: 35) must exceed the shutdown phases plus handoff.
- .NET `HostOptions.ShutdownTimeout` defaults to 30 s since .NET 8 (UNVERIFIED for 11; from learn.microsoft.com memory). Keep it under the Kubernetes grace period.

Register domain cleanup with `CoordinatedShutdown.Get(sys).AddTask(CoordinatedShutdown.PhaseClusterLeave, "flush-outbox", ...)`. Use a custom `handOffStopMessage` for entities that must flush before stopping (cluster-sharding docs §"Intercepting Actor Shutdown During Shard Re-Balancing Handoff").

### 4.4 Split Brain Resolver

- **Since 1.5.2 `keep-majority` is the default.** `stable-after = 20s`; `down-all-when-unstable = on` (3/4 of stable-after, min 4 s).
- The docs recommend:
  - `stable-after ≈ log10(maxNodes) * 10`.
  - Setting `down-removal-margin` equal to `stable-after`.
  - The default failover is about 45 s: 5 s detection + 20 s stable-after + 20 s margin.
- The docs also say: "If you are running in an environment where processes are not automatically restarted … we strongly recommend that you disable [down-all-when-unstable]". The corollary is that on Kubernetes, which restarts pods, you can keep it on.
- On Kubernetes, `lease-majority` with `Akka.Coordination.KubernetesApi` gives the strongest guarantee. It needs the `leases.akka.io` CRD plus RBAC (`akka.io` group, verbs get/create/update/list). Hosting API (VERIFIED compiles):

```csharp
SplitBrainResolver = new LeaseMajorityOption { LeaseImplementation = new KubernetesLeaseOption() }
```

---

## 5. Kubernetes: Deployment vs StatefulSet, discovery, probes

### 5.1 Recommendation

Petabridge, "Akka.NET guide to Kubernetes" (Aaron Stannard, 2025-10-24), and the "Zero-Downtime Akka.NET on Kubernetes" talk:

- **Deployments for new projects.** Use `strategy.rollingUpdate: { maxSurge: 1, maxUnavailable: 0 }`. A new pod joins before an old one leaves, so each pod's shards rebalance once. With a StatefulSet they rebalance twice.
- Deployments also give automatic rollback, and a failed liveness check doesn't wedge the rollout. A StatefulSet halts its rolling update on failed health.
- StatefulSet pros: stable DNS (`pod-0`) and static seed nodes without Akka.Management.
- With Deployments you **must** use Akka.Management + Cluster Bootstrap + discovery, because pod IPs are dynamic.
- Leave `HostName` blank or bind `0.0.0.0` and advertise the pod IP (`status.podIP`). Clear static seeds: `SeedNodes = []`.
- Use a headless service (`clusterIP: None`) with **`publishNotReadyAddresses: true`**. This is called "critical" to avoid a discovery/readiness deadlock: pods aren't Ready until they join, and they can't join until they discover each other.
- Petabridge bootstrap settings: `ContactWithAllContactPoints = true`, `StableMargin = 5s`, and a label selector such as `"app={0},environment=production"`.
- Probes: liveness `/healthz/live` (15/20/5/3) and readiness `/healthz/ready` (10/10/5/3). The numbers are initialDelay/period/timeout/failureThreshold.
- Example resources: 512Mi/500m requests, 4Gi memory limit, no CPU limit.

### 5.2 Node wiring (VERIFIED compiles against 1.5.73 and runs locally against Postgres 17)

`research/operations-snippets/Ops.Node/Program.cs`, abridged:

```csharp
builder.Services.AddAkka("orders", (akka, sp) =>
{
    akka.WithRemoting(new RemoteOptions { HostName = "0.0.0.0", PublicHostName = cfg["POD_IP"] ?? "localhost", Port = 5055 })
        .WithClustering(new ClusterOptions
        {
            Roles = ["orders"],
            AppVersion = appVersion,
            SeedNodes = inKubernetes ? [] : ["akka.tcp://orders@localhost:5055"],
            SplitBrainResolver = inKubernetes
                ? new LeaseMajorityOption { LeaseImplementation = new KubernetesLeaseOption() }
                : SplitBrainResolverOption.Default,
        })
        .WithActorSystemLivenessCheck()          // tags: akka, liveness
        .WithAkkaClusterReadinessCheck()          // tags: akka, ready, akka.cluster
        .WithSqlPersistence(cfg.GetConnectionString("journal")!, ProviderName.PostgreSQL15,
            autoInitialize: false,
            journalBuilder: j => j.WithHealthCheck().WithConnectivityCheck(),
            snapshotBuilder: s => s.WithHealthCheck());
    if (inKubernetes)
        akka.WithAkkaManagement(port: 8558)
            .WithClusterBootstrap(o =>
            {
                o.ContactPointDiscovery.ServiceName = "orders";
                o.ContactPointDiscovery.RequiredContactPointsNr = 2;
                o.ContactPointDiscovery.ContactWithAllContactPoints = true;
                o.ContactPointDiscovery.StableMargin = TimeSpan.FromSeconds(5);
            }, autoStart: true)
            .WithKubernetesDiscovery();       // default selector "app={0}", {0} = bootstrap service name
});
app.MapHealthChecks("/healthz/live",  new HealthCheckOptions { Predicate = c => c.Tags.Contains("liveness") });
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") || c.Tags.Contains("persistence") });
```

Observed locally (VERIFIED):

- `/healthz/live` and `/healthz/ready` both report Healthy once the node is Up. `/version` and `/pricing` (feature flag) respond.
- Postgres stopped, standard checks only: **ready remained Healthy** (gotcha).
- Postgres stopped, `WithConnectivityCheck()` added: ready went **Unhealthy** while live stayed Healthy.

Design note for the book: putting DB connectivity in *readiness* makes every pod unready during a DB outage, so the Service has no endpoints. That is usually what you want for a write-model service, because commands would fail anyway. Keep it **out of liveness**, or Kubernetes will restart-loop the whole cluster during a DB blip.

Kubernetes discovery defaults (`Akka.Discovery.KubernetesApi` README):

- `pod-label-selector = "app={0}"`, `use-raw-ip = true`.
- The namespace comes from the service account mount.
- Requires RBAC `pods: get, watch, list`.
- The management port (default 8558) must be exposed on the pod.

### 5.3 Manifests

`research/operations-snippets/k8s/` contains a ServiceAccount, a Role covering pods plus `akka.io/leases`, a RoleBinding, a headless `orders-akka` service (`publishNotReadyAddresses: true`; ports 8558/5055), an `orders-http` service, a Deployment (maxSurge 1 / maxUnavailable 0, grace 35, probes, `POD_IP` from `status.podIP`, `Akka__AppVersion`, connection string from a Secret) and a PodDisruptionBudget (`maxUnavailable: 1`). YAML parses with PyYAML, but **UNVERIFIED** on a real cluster, since no kubectl/kind is available.

Kustomize: `base/` plus an `overlays/prod` with `images: newTag` and a JSON patch for replicas and AppVersion. CI bumps the tag with `kustomize edit set image`.

Helm: the equivalent is `values.yaml` `image.tag`. Use `.Chart.AppVersion` as both the image tag and the `Akka__AppVersion` env var, so the cluster's AppVersion can't drift from the image. Chart `version` (SemVer of the chart) is independent of `appVersion`. UNVERIFIED (no helm installed).

**Do not run the CRD (`leases.akka.io`) from the app chart.** Install it cluster-wide once (`crds/` folder in Helm, or a separate kustomization).

---

## 6. Snapshots and migration

- Snapshots are an optimization; the journal is the truth.
- `snapshot-is-optional = false` is the default. Setting it true means a snapshot that fails to deserialize (e.g. after a shape change) is skipped and **all events are replayed**. The setting is present in 1.5.73 (checked in the DLL) and documented in `persistence.conf`.
  - Warning from the config: "Don't set to true if events are deleted", because state would be silently wrong.
- **Strategy for the book:**
  - Version snapshot types with their own manifest (`order-state.v3`), separate from events.
  - On a breaking snapshot change, either upcast in the serializer, or bump the manifest. Old rows throw `SerializationException`; with `snapshot-is-optional = true`, that falls back to full replay.
  - Then delete old snapshots offline. Keep `DeleteMessagesTo` off if you want this safety net.
  - Akka 1.6 docs: "A persisted event or snapshot is a durable contract."
- Persistence IDs are also a contract: `$"order-{id}"`. Renaming an aggregate type changes the prefix and orphans its history.

---

## 7. Projection rebuilds and versioned read models

- NuGet has **no** `Akka.Projections`, `Akka.Projection` or `Akka.Persistence.Projections` package (404 from the flat-container API). Akka.NET projections are hand-built:
  - A `ReadJournal` (`Akka.Persistence.Query`, `SqlReadJournal`), with `EventsByTag`/`AllEvents`/`EventsByPersistenceId`.
  - Akka.Streams.
  - Your own **offset store**: a table keyed by `(projection_name, projection_version)`.
- Run the projector as a cluster singleton or a sharded daemon process (`ShardedDaemonProcess`) so exactly one instance runs per partition.
- **Versioned read models (blue/green for projections):**
  1. Deploy `orders_summary_v2` (new table) with projection name `orders-summary` version `2`, starting at `Offset.NoOffset`.
  2. Let it catch up while v1 serves reads.
  3. Flip the read path, e.g. with a feature flag `ReadModel.OrdersSummaryV2`.
  4. Drop v1 later.
  - A rebuild never blocks writes. This only works because events are never deleted.
- Make projection handlers idempotent: upsert keyed by `(persistence_id, sequence_nr)`, or store the offset in the same transaction as the read-model write.
- Akka.Persistence.Sql 1.5.70 added `Offset.FromEnd(n)` for "last N events by tag".
- Akka.Persistence.Sql README, low-latency projection tuning: you must tune **both** `refresh-interval` and `journal-sequence-retrieval.query-delay`. The defaults give 500–1200 ms of lag; 10 ms/10 ms gives 20–100 ms at higher DB load.
- Tags are written at persist time. Adding a new tag to old events requires a write adapter for new events plus a one-off backfill, or a projection over `AllEvents` instead. Under TagTable mode, purging events by hand requires deleting tag rows too (see §10).

---

## 8. SemVer: services vs libraries vs message contracts

| Artifact | What SemVer means | Practical rule |
|---|---|---|
| **Library** (NuGet, e.g. `Orders.Contracts`) | Public API compatibility | Strict SemVer 2. Keep AssemblyVersion stable within a major (MinVer default `X.0.0.0` is right here) |
| **Service / container image** | Operability contract: config, endpoints, *rolling-update compatibility with N−1* | MAJOR = cannot roll from the previous version (needs blue/green or a full stop, e.g. a transport change). MINOR = new capability, rollable. PATCH = fix. AppVersion = `MAJOR.MINOR.PATCH` |
| **Message/event contracts** | Wire/persistence schema | Version **per message** via manifest (`order-placed.v2`), not per package. Events never get a "major" break because old versions remain readable forever |
| **Helm chart** | Chart `version` vs `appVersion` | Independent SemVers |
| **HTTP API** | Public API | URL or header versioning, separate concern |

---

## 9. Versioning tools: current state

| Tool | Version | Model | AssemblyVersion default | Notes |
|---|---|---|---|---|
| **MinVer** | 8.0.0 (8.0: SBOM in package; BREAKING: fails when default pre-release phase provided) | Tag-first. Untagged commits get `{next patch}-alpha.0.{height}` | `{Major}.0.0.0` | `MinVerTagPrefix=v`, `MinVerMinimumMajorMinor`. Needs `fetch-depth: 0`. "Height is not for versioning" |
| **Nerdbank.GitVersioning** | 3.10.94 | `version.json` + git height as patch | `major.minor` (`assemblyVersion.precision`) | `publicReleaseRefSpec`. `NBGV_GitEngine=Disabled` for shallow clones; auto-disabled under Copilot agent since 3.9. `nbgv` CLI, `ThisAssembly` class |
| **GitVersion** | 6.8.2 (MsBuild & Tool); GitTools/actions v4.7.0 | Branch-based config: `workflow: GitFlow/v1` (default), `GitHubFlow/v1`, `TrunkBased/preview1` (experimental) | `MajorMinorPatch` | Heaviest. Modes: ContinuousDelivery/ContinuousDeployment/ManualDeployment |

**Book recommendation:**

- Use **MinVer** with the `AssemblyVersion` target from §4.2, plus a `ContainerImageTags` target.
- Tag releases `vX.Y.Z`, ideally created by release-please (§11).

---

## 10. Database migrations for the SQL journal, retention and backups

### 10.1 Own the schema

The Akka.Persistence.Sql repo ships idempotent DDL (`docs/ddl/`):

- `default/` is for new deployments: tables `journal`, `tags`, `snapshot`, `journal_metadata`.
- `compat/` is for migrations from legacy plugins via `table-mapping = sql-server | postgresql | mysql | sqlite`. For example, the legacy Postgres layout is `event_journal`, snake_case.

**VERIFIED:** applying `journal.sql`, `journal-tags.sql` and `snapshot.sql` to `postgres:17` with psql created `journal`, `snapshot`, `tags`. The node then ran with `autoInitialize: false`.

- Disable auto-init (`auto-initialize = false` in HOCON, or `autoInitialize: false` in Hosting) so the app never needs DDL rights. Use a least-privilege DB user for the app.
- Run the DDL from FluentMigrator 8.0.1 (`Execute.EmbeddedScript`), DbUp 7.0.1, or an EF Core migration (`migrationBuilder.Sql(...)`). Examples are in `docs/ddl/README.md`.
- In Aspire, model this as a one-shot migrator project gated with `.WaitForCompletion(migrator)` (VERIFIED compiles, §13).
- **Optional FK** `tags.ordering_id → journal.ordering ON DELETE CASCADE` (Postgres/SQL Server). This is an operator-applied migration with orphan-cleanup scripts (`scripts/TagTableForeignKey`). Benchmark it before enabling.
- 1.5.73 adds opt-in `tagtable-asqueryable-insert-mode` (`off` | `parameterized` | `inline`) for batched tag writes.
- `delete-compatibility-mode` uses `journal_metadata`. The README warns it is "expensive". Disable it once off legacy plugins.
- Migration from legacy plugins (`docs/articles/migration.md`):
  1. Run in compatibility mode with the old tables.
  2. Optionally upgrade to TagTable.
  3. Enable the WriterUuid anti-corruption feature ("Recommended").

### 10.2 Retention (from `scripts/JournalRetention/README.md`)

Rules if you purge with SQL instead of `DeleteMessagesTo`:

1. Delete tags with their events, in one transaction.
2. **Keep each persistenceId's highest sequence number.** The plugin reads it to choose the next seq nr.
3. Don't delete events still needed for recovery (no newer snapshot) or by unread `EventsByTag` consumers.
4. Never delete tag rows whose event still exists.

`created` is .NET ticks. For a DDD/event-sourcing book, the default stance is **never delete events**. Use crypto-shredding for GDPR (UNVERIFIED pattern note).

### 10.3 Backups (general guidance; UNVERIFIED in this research)

- The journal is append-only, which suits Postgres PITR (base backup + WAL archiving, `pg_basebackup`).
- Use logical `pg_dump` for portable snapshots.
- Read models are derivable. Back them up for RTO, but they can always be rebuilt from the journal (§7).
- Restore drills:
  - Restore into a staging namespace.
  - Start the cluster with `autoInitialize: false`.
  - Rebuild projections and compare counts and hashes.
- After a restore to an earlier point in time, any **external side effects** (emails, payments) that happened after that point are not in the journal. Outbox/inbox idempotency keys matter.

---

## 11. Changelog, Conventional Commits and release-please

- Conventional Commits 1.0.0: `feat:`, `fix:`, `feat!:` or `BREAKING CHANGE:` footer. These map to MINOR/PATCH/MAJOR.
- `googleapis/release-please-action@v5` (v5.0.0, 2026-04-22; only breaking change node24; wraps release-please 17.6.0).
  - It has no .NET-specific release type. Use **`release-type: simple`** (`version.txt` + `CHANGELOG.md`) or manifest config.
  - It opens a release PR; merging it creates the `vX.Y.Z` tag. MinVer then picks the version up from the tag, so no csproj edits are needed.
  - Outputs: `releases_created`, `paths_released`, `pr`, `prs`.
- Pattern: release-please workflow on `main`, and a tag-triggered workflow that builds and pushes the image. UNVERIFIED end-to-end.

---

## 12. CI with GitHub Actions and container images

### 12.1 setup-dotnet v6.1.0 and .NET 11

- `dotnet-version: '11.0.x'` *includes* previews and RCs ("installs the latest patch … including prerelease versions (preview, rc)").
- Alternatively use `global-json-file: global.json`. For a pre-release `sdk.version` it installs the exact pinned version, ignoring rollForward.
- `dotnet-quality: preview` applies to *all* versions in one step. Use separate steps to mix a GA 10.0 with a preview 11.0.
- `cache: true` requires `packages.lock.json`. Use `cache-dependency-path` for subdirectories.
- Once .NET 11 is GA (expected Nov 2026), `11.0.x` resolves to GA automatically.

### 12.2 Example workflow

`research/operations-snippets/.github/workflows/ci.yml`; YAML-validated, not executed (**UNVERIFIED** on GitHub):

```yaml
- uses: actions/checkout@v7
  with: { fetch-depth: 0 }            # MinVer/NBGV/GitVersion need history + tags
- uses: actions/setup-dotnet@v6
  with: { global-json-file: global.json }
- run: dotnet restore --locked-mode
- run: dotnet build -c Release --no-restore
- run: dotnet test -c Release --no-build
- if: startsWith(github.ref, 'refs/tags/v')
  run: |
    echo "${{ secrets.GITHUB_TOKEN }}" | docker login ghcr.io -u "${{ github.actor }}" --password-stdin
    dotnet publish src/Orders.Node -c Release /t:PublishContainer \
      -p:ContainerRegistry=ghcr.io -p:ContainerRepository=${{ github.repository_owner }}/orders
```

### 12.3 SDK container publishing (VERIFIED with the RC3 SDK + podman)

`dotnet publish -c Release /t:PublishContainer -p:ContainerRepository=ops-verify -p:ContainerArchiveOutputPath=image.tar.gz`:

- Base image auto-selected: **`mcr.microsoft.com/dotnet/runtime:11.0.0-rc.3`** for a console app. Web SDK projects use `aspnet`.
- The default tag is **`latest`**, not the version.
- The image runs as non-root **UID 1654** (`app`), entrypoint `dotnet /app/Ops.Verify.dll`.
- OCI labels include `org.opencontainers.image.version`, `net.dot.runtime.majorminor=11.0` and the base digest.
- `podman load -i image.tar.gz` followed by `podman run` worked end to end. `ContainerArchiveOutputPath` needs no daemon. Pushing directly needs `ContainerRegistry` and docker/podman credentials.

Tag images from MinVer with a target, since a static property is evaluated before MinVer runs (VERIFIED: produced tags `1.5.0,1.5`):

```xml
<Target Name="TagImageWithMinVer" AfterTargets="MinVer">
  <PropertyGroup>
    <ContainerImageTags>$(MinVerVersion);$(MinVerMajor).$(MinVerMinor)</ContainerImageTags>
  </PropertyGroup>
</Target>
```

- SemVer build metadata (`+sha`) is not a legal image tag. `MinVerVersion` has no `+` part, but the InformationalVersion does.
- Other useful properties: `ContainerFamily` (e.g. chiseled variants), `ContainerBaseImage`, `ContainerPort`, `ContainerEnvironmentVariable` items and `ContainerLabel`. These are documented on learn.microsoft.com; not individually verified here.

---

## 13. Running locally with .NET Aspire

VERIFIED compiles: `Aspire.AppHost.Sdk/13.6.4` + `Aspire.Hosting.PostgreSQL 13.6.4`, net11.0, project-reference style.

```xml
<Project Sdk="Aspire.AppHost.Sdk/13.6.4"> ... <PackageReference Include="Aspire.Hosting.PostgreSQL" Version="13.6.4" />
```

```csharp
var builder = DistributedApplication.CreateBuilder(args);
var pgPassword = builder.AddParameter("pg-password", secret: true);
var postgres = builder.AddPostgres("postgres", password: pgPassword)
                      .WithDataVolume("orders-pgdata")
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithPgAdmin();
var journalDb = postgres.AddDatabase("journal");

var migrator = builder.AddProject<Projects.Ops_Verify>("migrator")
                      .WithReference(journalDb).WaitFor(journalDb);

builder.AddProject<Projects.Ops_Node>("orders")
       .WithReference(journalDb)                 // => ConnectionStrings__journal
       .WaitForCompletion(migrator)
       .WithEnvironment("Akka__AppVersion", "1.5.0")
       .WithEnvironment("FeatureManagement__NewPricing", "false");
builder.Build().Run();
```

- Builds with warning **ASPIRE010** ("configured with AspireUseCliBundle=false … Set AspireUseCliBundle=true"). Either set `<AspireUseCliBundle>true</AspireUseCliBundle>` or suppress it. The installed Aspire CLI is 13.6.3 against 13.6.4 packages.
- Postgres resource APIs in 13.6.4 include `WithInitFiles`, `WithCreationScript`, `WithDataVolume`, `WithPgAdmin`, `WithPgWeb` and `WithHostPort`.
- `WithInitFiles` runs via the image's init directory against the *default* DB, only on first volume creation. Prefer the migrator project for the journal DB.
- **Multi-replica clusters locally:**
  - `WithReplicas(n)` conflicts with a fixed remoting port and static seeds.
  - Use **`Akka.Aspire.Hosting` 1.5.73** in the AppHost: `builder.AddAkka("my-cluster").WithClustering(redis)`, then `.WithReference(akka)`.
  - In the service, use **`Akka.Aspire`**: `akkaBuilder.WithAspireClusterBootstrap(sp, configureDiscovery: ...)`.
  - Discovery runs through Redis or Azure Table locally, and `WithKubernetesDiscovery()` in production. Only that callback changes.
  - It registers a readiness check tagged `readiness`; note it is not `ready`.
  - Source: `Akka.Management/src/aspire/Akka.Aspire/README.md`. **UNVERIFIED locally.**

---

## 14. Feature flags (Microsoft.FeatureManagement 4.8.0)

- VERIFIED: `services.AddFeatureManagement()` with `IFeatureManager.IsEnabledAsync("NewPricing")`, driven by config (`FeatureManagement:NewPricing=true`, env `FeatureManagement__NewPricing`).
- Schemas:
  - The legacy `"FeatureManagement": { "X": true }` .NET schema still works.
  - **Since 4.0, variants and telemetry are only supported in the Microsoft schema**: `"feature_management": { "feature_flags": [ { "id": "X", "enabled": true, "conditions": { "client_filters": [...] } } ] }`.
- Built-in filters: `Microsoft.Percentage`, `Microsoft.TimeWindow`, `Microsoft.Targeting`.
- Use `IVariantFeatureManager` for variants. `[FeatureGate("X")]` lives in `Microsoft.FeatureManagement.Mvc`.
- Telemetry:
  - Requires `"telemetry": { "enabled": true }` per flag.
  - Emits on `ActivitySource` **`Microsoft.FeatureManagement`**: an Activity per evaluation plus an ActivityEvent `FeatureFlag`. Add the source to OpenTelemetry tracing. This ties into the instrumentation chapter.
- **DDD/actor rules for the book:**
  - Evaluate flags **only in command handling (decide)**, never in event application (evolve) or recovery. Replay must be deterministic regardless of today's flag values.
  - If the flag changes behaviour, record the outcome in the event (e.g. `PricingModel = "v2"`).
  - Actors shouldn't `await` the feature manager inside `Receive`. Evaluate in the API layer and pass the result in the command, or use `ReceiveAsync`/`PipeTo`.
  - Typical rollout uses: "emit new event version" (the second phase of §2.1), "read from projection v2" (§7), and dark-launching consumers.

---

## 15. Blue/green vs rolling for an event-sourced Akka cluster

| | Rolling (default) | Blue/green |
|---|---|---|
| When | Wire-compatible N−1 ↔ N (same transport, compatible messages) | Incompatible wire changes: classic ↔ Artery, ActorSystem name change, serializer swap without dual-read, major Akka upgrade with Wire rows |
| Mechanics | Deployment maxSurge 1; AppVersion steers shards to new nodes; rebalance paused while versions are mixed | Two separate clusters (different system name or discovery label), same journal DB |
| **Danger** | Mixed-version messages; mitigated by `SerializationException` + expand/contract | **Two clusters must never host the same entity at once.** Two writers to one persistenceId means sequence conflicts or interleaved histories |

Blue/green has to be done as a cut-over:

1. Stop blue's shard regions (drain).
2. Start green.
3. Switch traffic.

Downtime equals the drain plus the first recovery. The journal's unique `(persistence_id, sequence_number)` constraint and the WriterUuid feature are the last line of defence, not a plan. Projections can be blue/green independently (§7).

---

## 16. Suggested chapter outline (operations part)

1. Events are forever: serializer IDs, manifests, the default-JSON trap, unions vs persistence.
2. Evolving events: tolerant reader, upcasting in serializer vs adapter, golden-file tests.
3. Snapshots and read models: versioning, `snapshot-is-optional`, projection v2 cut-over.
4. Owning the database: DDL migrations, autoInitialize off, retention rules, backups and restore drills.
5. Versioning the service: SemVer layers, MinVer, AppVersion fix, Conventional Commits + release-please.
6. Building and shipping: GitHub Actions + setup-dotnet v6, SDK containers, tags.
7. Running in Kubernetes: Deployment + surge, Akka.Management bootstrap, RBAC, probes (connectivity gotcha), SBR/lease, coordinated shutdown budget, PDB, Kustomize/Helm.
8. Releasing safely: rolling-update mechanics (AppVersion, rebalance pause), expand/contract, feature flags, blue/green when you must.
9. Local production-like loop: Aspire AppHost, Postgres, migrator, Akka.Aspire multi-node.
10. Sidebar: upgrading to Akka.NET 1.6 (net10+, Hosting in-repo, serializer default bindings are a Wire change, Serialization.V2, Artery not wire-compatible).

---

## 17. Sources

**Akka.NET** (repo cloned at `dev` = 1.6.0-beta5 on 2026-11-04; 1.5.73 behaviour cross-checked against packaged DLLs):

- https://github.com/akkadotnet/akka.net/blob/dev/RELEASE_NOTES.md
- https://github.com/akkadotnet/akka.net/blob/dev/GETTING_STARTED_V1.6.md
- https://github.com/akkadotnet/akka.net/blob/dev/BREAKING_CHANGES_V1.6.md
- https://github.com/akkadotnet/akka.net/blob/dev/docs/articles/serialization/source-generated-serialization.md (Wire Format; Versioning and Rolling Upgrades)
- https://github.com/akkadotnet/akka.net/blob/dev/docs/articles/serialization/serialization.md (SerializationException guidance)
- https://github.com/akkadotnet/akka.net/blob/dev/docs/articles/serialization/codes.md
- https://getakka.net/articles/persistence/event-adapters.html
- https://github.com/akkadotnet/akka.net/blob/dev/docs/articles/clustering/split-brain-resolver.md
- https://github.com/akkadotnet/akka.net/blob/dev/docs/articles/clustering/cluster-sharding.md
- https://github.com/akkadotnet/akka.net/blob/dev/docs/articles/actors/coordinated-shutdown.md
- https://github.com/akkadotnet/akka.net/blob/dev/docs/articles/hosting/health-checks.md

**Akka.NET source files:**

- `src/core/Akka.Cluster/Configuration/Cluster.conf`
- `src/core/Akka/Util/AppVersion.cs`
- `src/contrib/cluster/Akka.Cluster.Sharding/Internal/AbstractLeastShardAllocationStrategy.cs`
- `src/contrib/cluster/Akka.Cluster.Tools/Singleton/reference.conf`
- `src/core/Akka.Persistence/persistence.conf`
- `src/contrib/hosting/Akka.Hosting/AkkaHostedService.cs`
- `src/contrib/hosting/Akka.Cluster.Hosting/AkkaClusterHostingExtensions.cs`

**Akka.Management:**

- https://github.com/akkadotnet/Akka.Management (docs/articles/discovery/kubernetes.md, src/discovery/kubernetes/Akka.Discovery.KubernetesApi/README.md, src/coordination/kubernetes/Akka.Coordination.KubernetesApi/README.md, src/aspire/Akka.Aspire/README.md, RELEASE_NOTES.md)

**Akka.Persistence.Sql** (`v1.5` and `dev` branches):

- https://github.com/akkadotnet/Akka.Persistence.Sql (README.md, RELEASE_NOTES.md, docs/ddl/README.md, docs/ddl/default/postgresql/*.sql, docs/articles/migration.md, scripts/JournalRetention/README.md)

**Petabridge:**

- https://petabridge.com/blog/akkadotnet-guide-to-kubernetes/ (2025-10-24)
- https://aaronstannard.com/presentations/2025/akka-k8s-zero-downtime/

**Versioning tools:**

- MinVer: https://github.com/adamralph/minver (README, CHANGELOG)
- Nerdbank.GitVersioning: https://github.com/dotnet/Nerdbank.GitVersioning/blob/main/docfx/docs/versionJson.md and `shallow-cloning-agents.md`
- GitVersion: https://github.com/GitTools/GitVersion/blob/main/docs/input/docs/reference/configuration.md and https://gitversion.net/docs/reference/configuration

**GitHub Actions:**

- https://github.com/actions/setup-dotnet (README)
- https://github.com/googleapis/release-please-action (README, v5.0.0 release notes)

**Microsoft.FeatureManagement:**

- https://learn.microsoft.com/en-us/azure/azure-app-configuration/feature-management-dotnet-reference (updated 2026-06-22)

**NuGet versions:**

- `https://api.nuget.org/v3-flatcontainer/<id>/index.json` (queried 2026-11-04)
- GitHub releases API for action versions
