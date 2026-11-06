# Research notes: DDD, actors and event sourcing in a .NET MAUI client

Research date: 2026-11-04. Toolchain: .NET SDK 11.0.100-rc.3.26522.148 (the book's local SDK), C# 15.
Everything marked **VERIFIED** was compiled or run on this machine (jfw16f, Fedora 44, x64). Anything
marked **UNVERIFIED** comes from reasoning or secondary sources and should be checked before it is
printed. All probe code lives in
`/tmp/claude-1000/-home-jonathan-code-scienceNight/45f474fc-e89c-490e-8b5f-ec3ffd186198/scratchpad/mobile-maui-exp/`.
That is a session scratch folder, so copy anything worth keeping into `sample/`.

---

## 0. The answer in one paragraph

Keep the domain model (aggregates, invariants, event sourcing, sharding) on the server. The phone is
a separate bounded context: a presentation and offline context. It sits downstream of the server's
Open Host Service and talks to it only through the Published Language (a small Contracts package of
DTOs). Several DDD ideas *do* pay off on the device:

- the ubiquitous language in the UI
- value objects for input validation
- command objects with client-generated IDs that double as idempotency keys
- a durable local outbox of pending commands
- optimistic UI that reconciles against server-pushed read-model changes
- UI and command state modelled as C# 15 unions

Actors as a *shape* are useful on the client: one mailbox, one reader, no locks. A
`System.Threading.Channels` loop gives you that shape without a framework. Akka.NET *can* run inside a
MAUI app, but on phones it is the wrong tool today. It is worth considering for desktop, kiosk and edge
devices, and becomes more reasonable once Akka.NET 1.6 ships its AOT work.

---

## 1. Where DDD and actor concepts belong

### 1.1 Server: the domain lives here

The server already has the things that make DDD and actors pay off:

- **Aggregates with invariants that span users and devices.** Only one place can decide whether
  "add line 21 to an order capped at 20 lines" is legal. That place is the single-writer aggregate
  behind cluster sharding, where check-then-persist is race-free because a sharded entity processes
  one message at a time.
- **Event sourcing.** The journal is the system of record. Projections build read models from it.
- **Distribution.** Sharding, passivation, rebalancing and supervision only make sense on servers.

### 1.2 Client: a separate bounded context

Use Evans's context-mapping vocabulary explicitly in the chapter:

| Relationship | Server role | Client role |
|---|---|---|
| Open Host Service + Published Language | ASP.NET Core API (HTTP + SignalR/SSE) publishing a versioned contract | consumer |
| Customer/Supplier | upstream (supplier) | downstream (customer). The mobile team can ask for API changes, such as a `?sinceVersion=` catch-up endpoint. |
| Anticorruption Layer (thin) | – | the client's `ICommandSender` / read-cache code translates Contracts DTOs into the client's own view-state types |

The client has its own model and its own language for things the server never sees:

- "pending", "syncing", "failed to sync", "offline"
- draft forms
- local-only preferences

That is a legitimate bounded context. It is not a degraded copy of the server domain.

### 1.3 Why NOT to share the domain model or aggregates with the client

Arguments for the chapter:

1. **The client is untrusted.** Any invariant checked on the device must be re-checked on the server,
   so a shared aggregate gives you two enforcement points and only one of them counts. Client checks
   are UX, not correctness.
2. **Version skew.** Phones run old builds for months; app-store review and user update habits mean
   the server must accept N-1 and N-2 clients. A shared `Order` aggregate assembly couples the
   release cadence of the domain to the slowest app-store channel. Contracts can be versioned
   additively, but aggregates can't.
3. **The client sees a projection, not the aggregate.** The device holds read models built for screens
   (denormalised, possibly cross-aggregate), never the event stream. An aggregate on the device would
   either be empty or would need the full event history, and that history is not the client's to have.
   It may contain other users' data or PII, and it may be large.
4. **Different consistency model.** On the server, an aggregate decides synchronously and atomically.
   On the device, everything is provisional until the server accepts it. Pretending the device's copy
   decides creates "it said OK and then it wasn't" bugs.
5. **Packaging and trimming.** Domain-core assemblies tend to pull in server dependencies over time.
   Even a "pure" core gets an Akka reference for messages, or Newtonsoft attributes. Every one of
   them lands in the APK/IPA and has to be trim- and AOT-safe (see §2).

**What to share:** a `*.Contracts` project (net11.0, `IsAotCompatible=true`, no dependencies)
holding:

- request/command DTOs that carry a `CommandId`
- reply DTOs (`CommandAccepted`, `CommandRejected` with a machine-readable `Code`)
- read-model/notification DTOs
- a source-generated `JsonSerializerContext`, so serialisation is trim/AOT-safe on the device

Value-object-ish *shapes* such as `string Sku` or `int Quantity` are fine to share. Validation
*behaviour* is duplicated on purpose. The client copy exists for fast feedback; the server copy is
authoritative.

Publish the Contracts package to a feed, or reference it as a project inside the repo. The book can
keep it as a ProjectReference and add one sentence on versioning:

- add fields, never remove or rename them
- the server tolerates missing fields
- the client tolerates unknown `Code` values

### 1.4 DDD ideas that DO pay off on the device

| Idea | On the device | Verified in probe |
|---|---|---|
| Ubiquitous language in the UI | Labels, rejection messages and state names use domain terms ("Order submitted on another device", not "409") | – |
| Value objects for input | `Quantity.TryParse(text, out q, out error)`: invalid input never becomes a command | yes |
| Command objects with client-generated IDs | `AddLineRequest(Guid CommandId, …)` with `Guid.CreateVersion7()`, which is time-ordered and so sorts well in SQLite and logs | yes |
| Durable outbox | Write the command to SQLite *before* the network call; a dispatcher drains it | yes |
| Optimistic UI and reconciliation | Show pending lines as an overlay on the server read model; drop the overlay when a pushed read-model version ≥ the accepted version | design only |
| Read models pushed to the client | Projection → SignalR/SSE notification `OrderSummaryChanged(OrderId, Version, …)` → local cache upsert-if-newer | design only |
| UI state as unions/state machines | `union CommandStatus(Queued, Accepted, Rejected)`; the compiler enforces exhaustive switches | yes |

**C# 15 unions (VERIFIED on 11.0.100-rc.3):**

- `public union CommandStatus(Queued, Accepted, Rejected);` compiles with the default language
  version for `net11.0`. No `LangVersion=preview` is needed.
- Implicit conversion from each case works: `CommandStatus s = new Queued(0);`
- Leaving out a case in a `switch` expression produces **CS8509** ("does not handle all possible
  values… the pattern 'Rejected' is not covered").
- C# 15 also adds `closed` class hierarchies (`public closed record GateState;`). This is the
  alternative when the cases need a shared base, for example a common `CommandId` property.

Source: https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-15 (dated 2026-10-19). It confirms
unions and closed hierarchies are C# 15 and do not need preview mode.

---

## 2. Actors on the device

### 2.1 Platform facts for .NET 11 (primary sources)

From https://learn.microsoft.com/dotnet/maui/whats-new/dotnet-11 and
https://learn.microsoft.com/dotnet/maui/deployment/trimming:

- **CoreCLR is the default runtime** for .NET 11 MAUI on Android, iOS and Mac Catalyst; Mono is still
  available. NativeAOT is an opt-in publishing alternative and is marked **experimental on Android**.
- On iOS and Mac Catalyst, CoreCLR uses composite ReadyToRun plus an **always-on interpreter** for
  code that isn't precompiled, because JIT is not allowed. CoreCLR on Apple platforms defaults to
  `DynamicCodeSupport=true`, which keeps `Reflection.Emit` in trimmed output. NativeAOT defaults to
  `false`.
- **Trimming defaults:** Android and Mac Catalyst Release builds use `TrimMode=partial`. iOS uses
  partial trimming for all device builds. Partial mode trims only the BCL, platform assemblies and
  assemblies marked `IsTrimmable`. Third-party assemblies without that marker, Akka among them, are
  kept whole.
  - **VERIFIED** with `dotnet msbuild -getProperty:TrimMode` on a `net11.0-android` Release project:
    the value is `partial`, and `UseMonoRuntime` is empty, meaning CoreCLR.
- Android CoreCLR ReadyToRun requires `PublishTrimmed=true`. The minimum Android API is now **24**,
  and Android x86 is dropped.
- iOS and Mac Catalyst apps built with the Xcode 27 SDKs must use the UIKit **scene lifecycle**; new
  MAUI 11 templates include it.

### 2.2 Akka.NET inside MAUI: evidence

**Experiments (VERIFIED, linux-x64, which stands in for the trimming and AOT toolchain):** a minimal
`ActorSystem.Create` → `ReceiveActor` → `Ask` → `Terminate` console app, published several ways.

| Akka version | Publish mode | Trim warnings | Runs? |
|---|---|---|---|
| 1.5.73 (latest stable, 2026-10-05) | `PublishTrimmed` full | 3× IL2104 (Akka, Newtonsoft.Json, System.Configuration.ConfigurationManager) | **No.** Crashes in `ActorSystem.Create` → `ConfigurationFactory.Load()` with `ConfigurationErrorsException: Configuration system failed to initialize` (`MissingMethodException` for `ClientConfigurationHost`) |
| 1.5.73 | `TrimMode=partial`, which is what MAUI Android/iOS use by default | 3 | **Yes** |
| 1.5.73 | `PublishAot` | IL2104 ×2 + IL3053 | **No.** Same `ConfigurationErrorsException` |
| 1.6.0-beta5 (released 2026-11-04) | `PublishTrimmed` full | 0 | **Yes** |
| 1.6.0-beta5 | `PublishAot` | IL2104 ×2 (Akka, Newtonsoft) + IL3053 | **Yes** |

The 1.5.73 crash is exactly the error in the long-open issue
https://github.com/akkadotnet/akka.net/issues/5153, "Akka with Xamarin Error 'Configuration system
failed initialize'" (opened 2021, still open).

Size: an untrimmed Akka 1.5.73 closure adds about 2.4 MB of managed DLLs:

- Akka.dll 947 KB
- Newtonsoft.Json 695 KB
- System.Configuration.ConfigurationManager 396 KB
- System.Drawing.Common, System.Security.Permissions and similar Windows-compat shims, pulled in
  through ConfigurationManager

Under partial trimming none of these are trimmed. A blank MAUI Android APK is already tens of MB, so
this is not decisive, but it is pure overhead for what a Channel loop does for free.

**Primary-source issue trail:**

- **Epic: Full AOT Support for Akka.NET** (https://github.com/akkadotnet/akka.net/issues/7246, open).
  It names "Mobile: iOS / Android" as a v1.6 goal. The maintainer says core Akka is AOT-capable "so
  long as you don't use `ReceiveActor`s… Them and the old System.ConfigurationManager bits". Our
  1.6.0-beta5 AOT probe used a `ReceiveActor` and ran, so the beta has moved past that comment.
  It was still a trivial probe.
- Active AOT work as of October 2026:
  - #8605, "AOT (5/7): … generic Expression.Lambda in the Props argument parser", merged 2026-09-25
  - #8626 and #8696, serializer registration in code
  - #8718, Remote/Cluster AOT canary
  - #8753, persistence plugins opt in to Native AOT
- **Akka.Hosting does not work in MAUI.** MAUI never starts `IHostedService`s (dotnet/maui#2244,
  "Add support for hosted services in .NET MAUI apps", open since 2021-08-25). `AddAkka` therefore
  throws `PlatformNotSupportedException` inside a MAUI app
  (https://github.com/akkadotnet/akka.net/issues/8795 and #8794).
  - The only `Akka.Hosting.Maui` package on NuGet is **1.5.8**, which targets net6.0 MAUI.
  - The replacement, `AddAkkaMaui` for 1.6 plus a MAUI NativeAOT canary on Mac Catalyst, is **draft
    PR #8790, unmerged as of 2026-10-07**.
  - In practice: in a MAUI app, call `ActorSystem.Create` yourself in `MauiProgram` and register the
    system as a singleton. Do not use `AddAkka`.
- Historical: #6459 (`ActorSystem.Create` threw `PlatformNotSupportedException` on net7.0-android
  because of a `Console.BackgroundColor` hack) was fixed in 2023.

**App lifecycle** (https://learn.microsoft.com/dotnet/maui/fundamentals/app-lifecycle):

- `Window.Stopped` maps to Android `OnStop` and iOS `SceneDidEnterBackground`. The docs say "There's
  no guarantee that an app will resume from this state, because it may be terminated by the operating
  system", and the recommended action there is to "disconnect from any long running processes".
- `Destroying` may never be delivered before the OS kills a backgrounded process.

The consequence is that **in-memory actor state is not durable on a phone.** Any mailbox contents,
in-flight Asks or actor state disappear when the process dies. You could add Akka.Persistence on
the device with a SQLite journal, but it would be a second event store, with its own schema and
plugin, inside a process that dies regularly. It is the server's job to be the event store.

**Battery and threads:**

- Akka's default scheduler is `HashedWheelTimerScheduler` with `akka.scheduler.tick-duration = 10ms`.
  **VERIFIED** in the embedded reference config of both 1.5.73 and 1.6.0-beta5.
- That is a dedicated timer thread waking 100 times a second while it runs. Whether it parks when
  nothing is scheduled is **UNVERIFIED**.
- Akka also starts dispatcher threads. The default dispatcher is a thread pool, so it shares the .NET
  pool.
- None of this is fatal on a desktop. On a phone it is a reason to stop the ActorSystem on `Stopped`
  and recreate it on `Resumed`. That in turn means actor state must be rebuildable from local
  storage, which is the same work as not using actors.

**What was NOT verified:**

- Akka running on a real Android device or emulator: no Android SDK on this machine (see §5).
- Akka on iOS under the CoreCLR interpreter.
- Akka.Remote or Cluster from a phone. Don't do this: a phone is not a cluster member, and Akka.Remote
  over cellular NAT is unsupported in practice (#4191 discusses NAT hole punching).

### 2.3 Lighter actor-shaped options for the client

| Option | Verdict for a phone |
|---|---|
| **`System.Threading.Channels` single-reader loop** | **Recommended.** One `Channel<T>` with `SingleReader = true` as the mailbox, one `await foreach` loop as the only code that mutates the component's state. It has the Akka properties that matter (serialised processing, no locks, "Tell") with zero dependencies, it is trim/AOT-safe, and it is trivially restartable on `Resumed`. VERIFIED in the probe (`OutboxDispatcher`, §3.4). |
| Hand-rolled agent (`MailboxProcessor`-style class around a Channel) | Same thing with a reusable base class. Fine if the book wants a "here's the actor shape without the framework" sidebar. About 40 lines. |
| **Akka.NET 1.5.x** | Runs under MAUI's default partial trimming (verified on linux-x64 only). Breaks under full trimming and NativeAOT, Akka.Hosting doesn't work in MAUI, and it adds about 2.4 MB plus a 10 ms timer thread. **No for phones.** |
| **Akka.NET 1.6 (beta5)** | Trims and AOTs cleanly in our trivial probe; MAUI hosting support is pending (#8790). **Plausible for desktop, kiosk or edge MAUI and Avalonia apps once 1.6 is stable**, especially an edge device that already speaks to an Akka cluster over a trusted LAN. Still not a phone recommendation. |
| **Proto.Actor 1.8.0** (net8.0) | Core depends on Google.Protobuf, M.E.DependencyInjection, System.Text.Json and others. Lighter than Akka, but there is no compelling reason to add it to a phone app. Trim and AOT status: **UNVERIFIED**. |
| **Microsoft Orleans client** (Microsoft.Orleans.Client 10.4.2) | **Not on device.** The Orleans client is a co-hosted or gateway client for trusted networks: it connects to silos over the cluster transport. Phones talk to an API in front of Orleans, exactly like the Akka setup here. |

**Nuanced verdict for the chapter:**

- **Phones:** actor *thinking* yes, actor *framework* no. Use a Channel loop.
- **Kiosks, desktop, factory-floor and edge boxes** (long-lived process, mains power, trusted
  network, maybe an intermittent uplink): a local Akka system can be a good fit, and with 1.6 it
  becomes AOT-friendly. That is a sidebar, not the main path.

---

## 3. Sync and offline patterns

### 3.1 End-to-end flow

```
[UI] --validate (value object)--> AddLineRequest{CommandId=v7 Guid}
     --INSERT OR IGNORE--> SQLite outbox (status=queued)        <-- durable before any I/O
     --Nudge()--> OutboxDispatcher mailbox (Channel, single reader)
         if offline: stop
         POST /orders/{id}/lines  (body has CommandId; optionally Idempotency-Key header)
           API -> sharding region -> OrderActor (single writer)
              duplicate CommandId?  -> reply Accepted(current version), persist nothing
              invalid?              -> reply Rejected(code, reason)
              valid                 -> Persist(LineAdded{CommandId,...}) -> reply Accepted(version)
           <- 200 CommandAccepted | 409/422 ProblemDetails(code) | timeout/5xx
         Accepted  -> outbox row accepted(version); UI overlay waits for read model >= version
         Rejected  -> outbox row rejected; UI shows reason in domain language; NEVER retried
         Transient -> attempts++; stop draining (preserve order); retry on next Nudge
                      (timer/backoff, ConnectivityChanged, Window.Resumed)
Projection: LineAdded -> OrderSummary read model -> hub/SSE push OrderSummaryChanged{Version}
Client: upsert cache row if Version > cached; drop pending overlays with acceptedVersion <= Version
On reconnect/resume: GET /orders/{id}/summary (or ?sinceVersion=) to catch up missed pushes
```

### 3.2 Why idempotency is mandatory, and how the aggregate does it

The story the chapter should tell: the API does `Ask` to the shard region with a timeout. The
aggregate may persist *after* the HTTP request has already timed out, or the phone may lose the
response on a flaky network. The client cannot tell "not done" from "done, but I didn't hear back",
so it **must** retry with the same `CommandId`, and the server **must** dedupe.

Server design (VERIFIED in `Shop.Server.Probe`, Akka.Persistence 1.5.73, default in-memory journal):

- The **event carries the CommandId**: `LineAdded(Guid CommandId, string Sku, int Quantity)`.
  Dedupe state is therefore rebuilt on recovery and is *atomic with* the persisted event. A separate
  "processed commands" table written after the event would not be atomic.
- A pure decider returns Duplicate, Rejected or an Event. The actor replies to a duplicate exactly as
  it replied the first time, `CommandAccepted(CommandId, OrderId, currentVersion)`, and persists
  nothing.
- **Single writer makes this race-free.** Cluster sharding guarantees one live entity per id, and the
  actor processes one message at a time, so check-then-persist needs no locks. This is the one place
  in the chapter where the actor model directly earns its keep for mobile.
- The dedupe set must be **bounded**. The probe keeps the last 1,000 command ids, evicting the oldest
  as `Apply` adds new ones. Alternatives:
  - a time window that is longer than the client's maximum outbox retry age
  - storing the dedupe set in snapshots
- Document the contract: the client stops retrying after X days and marks the command "failed to
  sync", and the server remembers ids for longer than X.

Verified test: send the same command twice, stop the actor, recreate it so it recovers from the
journal, and send it a third time. All three replies are `Accepted(Version=1)` and the final version
is 1 (exactly one event). A second test checks that quantity 500 is rejected with code
`bad-quantity`. Both pass.

```csharp
// Pure domain core
public sealed record LineAdded(Guid CommandId, string Sku, int Quantity);
public static class OrderDecider
{
    public static Decision Decide(OrderState s, AddLineRequest c) =>
        s.SeenCommands.Contains(c.CommandId) ? new(null, null, null, Duplicate: true)
        : s.Lines.Count >= OrderState.MaxLines ? new(null, "too-many-lines", $"An order holds at most {OrderState.MaxLines} lines.", false)
        : c.Quantity is < 1 or > 99 ? new(null, "bad-quantity", "Quantity must be 1-99.", false)
        : new(new LineAdded(c.CommandId, c.Sku, c.Quantity), null, null, false);
}

// Actor shell (ReceivePersistentActor)
Command<AddLineRequest>(cmd =>
{
    var d = OrderDecider.Decide(_state, cmd);
    if (d.Duplicate)
        Sender.Tell(new CommandAccepted(cmd.CommandId, cmd.OrderId, _state.Version));
    else if (d.Event is { } e)
        Persist(e, ev => { _state = _state.Apply(ev);
                           Sender.Tell(new CommandAccepted(cmd.CommandId, cmd.OrderId, _state.Version)); });
    else
        Sender.Tell(new CommandRejected(cmd.CommandId, cmd.OrderId, d.RejectCode!, d.RejectReason!));
});
```

HTTP mapping to recommend:

| Result | HTTP response | Client treatment |
|---|---|---|
| Accepted | `200` with `CommandAccepted` (or `202` if the book's API is async) | done |
| Rejected | `409 Conflict` or `422` with ProblemDetails carrying `code` | terminal |
| Ask timeout | `504`/`503` | transient, retry |
| Other `5xx` or network failure | – | transient, retry |

Optionally accept an `Idempotency-Key` header, which is the IETF httpapi draft "The Idempotency-Key
HTTP Header Field". The draft's standards status is **UNVERIFIED**. If you accept the header, it must
equal the body's `CommandId`. Keeping the id *in the body* makes it part of the Published Language,
and that is the cleaner DDD story.

### 3.3 Conflicts and rejections

- **Commutative commands** (add line, add comment) need no expected version; dedupe is enough.
- **Commands that depend on what the user saw** (change price, rename, submit) should carry
  `ExpectedVersion` taken from the read model the user was looking at. The aggregate rejects with
  `code = "stale"` when it doesn't match. The client keeps the user's input, refreshes the read
  model, and asks the user to confirm again. Never auto-resolve silently.
- **A rejected command is terminal.** Never re-send it. Mark it, surface it in domain language, and
  offer "edit and retry", which creates a *new* CommandId.
- **Dependent queued commands:** if command 1 is rejected, commands 2..n for the same aggregate may
  now be meaningless. The simple, honest policy is to keep sending them and let the server reject
  them too, since the server is the authority. A smarter client could pause the aggregate's queue
  and ask the user. Keep the book on the simple policy.
- **Ordering:** the dispatcher stops at the first *transient* failure, so per-device order is
  preserved. Rejected commands do not block the queue.

### 3.4 The client outbox and dispatcher (VERIFIED)

`Shop.Client` targets plain `net11.0` with `IsAotCompatible=true` and `TreatWarningsAsErrors=true`. It
builds with **zero warnings** and references:

- CommunityToolkit.Mvvm 8.4.2
- Microsoft.Data.Sqlite 11.0.0-rc.3.26522.148, which brings SQLitePCLRaw.bundle_e_sqlite3 3.0.5
- the Contracts project

Eight xunit.v3 tests pass (§5.4).

```csharp
public sealed record Queued(int Attempts);
public sealed record Accepted(long Version);
public sealed record Rejected(string Code, string Reason);
public union CommandStatus(Queued, Accepted, Rejected);

public sealed record SendAccepted(long Version);
public sealed record SendRejected(string Code, string Reason);
public sealed record SendTransientFailure(string Why);
public union SendOutcome(SendAccepted, SendRejected, SendTransientFailure);
```

`SqliteOutbox` notes:

- One table, `outbox(command_id TEXT PRIMARY KEY, order_id, kind, payload, created_utc, attempts,
  status, version, code, reason)`, using `PRAGMA journal_mode = WAL`.
- Enqueue uses `INSERT OR IGNORE`, so a double tap with the same CommandId is a no-op. That is
  tested.

The dispatcher is the "actor without a framework":

```csharp
public sealed class OutboxDispatcher : IAsyncDisposable
{
    private abstract record Signal;
    private sealed record Kick : Signal;
    private sealed record Drained(TaskCompletionSource Done) : Signal;

    private readonly Channel<Signal> _mailbox = Channel.CreateUnbounded<Signal>(new() { SingleReader = true });
    // ... ctor starts: _loop = Task.Run(RunAsync);

    public void Nudge() => _mailbox.Writer.TryWrite(new Kick());

    private async Task RunAsync()
    {
        await foreach (var signal in _mailbox.Reader.ReadAllAsync(_stop.Token))
            switch (signal)
            {
                case Kick: await DrainAsync(_stop.Token); break;
                case Drained d: d.Done.TrySetResult(); break;
            }
    }

    private async Task DrainAsync(CancellationToken ct)
    {
        if (!_connectivity.IsOnline) return;
        foreach (var cmd in await _outbox.ReadQueuedAsync(ct))
        {
            await _outbox.MarkAttemptAsync(cmd.CommandId, ct);
            switch (await _sender.SendAsync(cmd, ct))
            {
                case SendAccepted a:  await _outbox.MarkAcceptedAsync(cmd.CommandId, a.Version, ct);
                                      StatusChanged?.Invoke(cmd.CommandId, new Accepted(a.Version)); break;
                case SendRejected r:  await _outbox.MarkRejectedAsync(cmd.CommandId, r.Code, r.Reason, ct);
                                      StatusChanged?.Invoke(cmd.CommandId, new Rejected(r.Code, r.Reason)); break;
                case SendTransientFailure: return;   // keep order; retry same CommandId later
            }
        }
    }
}
```

The ViewModel follows CommunityToolkit.Mvvm 8.4 style with C# partial properties. This compiles on
C# 15 without preview mode.

```csharp
public sealed partial class OrderLineViewModel(SqliteOutbox outbox, OutboxDispatcher dispatcher, string orderId) : ObservableObject
{
    [ObservableProperty] public partial string Sku { get; set; } = "";
    [ObservableProperty] public partial string QuantityText { get; set; } = "1";
    [ObservableProperty] public partial string? ValidationError { get; set; }

    [RelayCommand]
    private async Task AddLineAsync()
    {
        if (!Quantity.TryParse(QuantityText, out var qty, out var error)) { ValidationError = error; return; }
        ValidationError = null;
        var request = new AddLineRequest(Guid.CreateVersion7(), orderId, Sku, qty.Value);
        var json = JsonSerializer.Serialize(request, ContractsJsonContext.Default.AddLineRequest);
        await outbox.EnqueueAsync(request.CommandId, orderId, "AddLine", json);  // durable first
        dispatcher.Nudge();                                                      // then try to send
    }
}
```

Production gaps for the chapter to mention (not in the probe):

- **Retry backoff:** a `PeriodicTimer` or Polly via `Microsoft.Extensions.Http.Resilience` on the
  `HttpClient`, with jitter.
- **Maximum age, then "failed to sync":** described in §3.2.
- **Marshal `StatusChanged` to the UI thread:** use `MainThread.BeginInvokeOnMainThread` in the MAUI
  head, or an `IDispatcher` abstraction in the library.
- **Garbage-collect old accepted rows.**
- **Encrypt the database if the payload is sensitive.** sqlite-net-pcl exposes iOS file-protection
  flags. Microsoft.Data.Sqlite has no equivalent built in, so use an SQLCipher bundle; **UNVERIFIED**
  for SQLitePCLRaw 3.x.

### 3.5 Event-driven read-model sync

- **Push:** after the projection writes `OrderSummary`, publish `OrderSummaryChanged(OrderId, Version,
  …)` to a SignalR group per order or user. ASP.NET Core 10+ also offers `TypedResults.ServerSentEvents`
  as an SSE alternative; SignalR is the better fit for MAUI because of its .NET client, reconnect
  handling and tracing (§4).
- **Client cache:** a SQLite table `order_summary(order_id PK, version, json)` with an
  `UPDATE … WHERE version < $v` upsert (last-writer-by-version). Pushes may arrive out of order or be
  duplicated, and version comparison makes both harmless.
- **Reconciliation:** the screen shows `cached summary` merged with `outbox rows for this order where
  status=queued, or status=accepted with version > cached version`. Once the read model catches up,
  the overlay disappears. This is the "optimistic UI" without lying: the pending badge stays until the
  *projection*, not just the aggregate, reflects the change.
- **Missed pushes:** SignalR does not replay messages missed while disconnected. With
  `WithAutomaticReconnect()` the default retry delays are 0, 2, 10 and 30 s, then the client gives up
  and fires `Closed`. "Because the connection looks entirely new to the server, a new `ConnectionId`
  is provided to the `Reconnected` event handlers"
  (https://learn.microsoft.com/aspnet/core/signalr/dotnet-client).
  - So on `Reconnected`, on app `Resumed`, and after the initial start, re-join the groups and
    **pull** current summaries, for example `GET /orders/{id}/summary`, or a `?sinceVersion=` feed
    if the projection keeps one.
  - `WithAutomaticReconnect` doesn't retry the *initial* start, so wrap `StartAsync` in a retry loop.
    The docs give one.
- **Background:** neither OS keeps a WebSocket alive for a backgrounded app.
  - On `Window.Stopped`, stop the hub connection and stop draining.
  - On `Resumed`, restart both and catch up.
  - Use FCM/APNs push notifications if the user must hear about something while the app is closed.
    That is out of scope for the chapter; mention it in one paragraph.

### 3.6 Local storage choice

| | sqlite-net-pcl 1.11.285 | Microsoft.Data.Sqlite 11.0.0-rc.3.26522.148 |
|---|---|---|
| MAUI docs use it | yes (https://learn.microsoft.com/dotnet/maui/data-cloud/database-sqlite); the docs name Microsoft.Data.Sqlite as the alternative | – |
| Native bundle | SQLitePCLRaw 3.0.3 (+ SourceGear.sqlite3 3.53.3; iOS group uses system sqlite3 provider) | SQLitePCLRaw.bundle_e_sqlite3 3.0.5 |
| Style | reflection-based mini-ORM (attributes, `Table<T>()`) | ADO.NET, explicit SQL |
| Trim/AOT | reflection ORM, so risky under full trim (**UNVERIFIED**) | no warnings with `IsAotCompatible=true` in our build (VERIFIED, library-level) |

**Recommendation:** use Microsoft.Data.Sqlite for the book. Explicit SQL makes the outbox and
version-guarded upserts visible, it is the same provider family readers know from the server, and it
ships with .NET 11 versions. The DB path in the MAUI head should be
`Path.Combine(FileSystem.AppDataDirectory, "shop.db3")`, passed into the library as a connection
string so the library stays MAUI-free.

### 3.7 Connectivity APIs

Source: https://learn.microsoft.com/dotnet/maui/platform-integration/communication/networking

- `Microsoft.Maui.Networking.IConnectivity`. Use `Connectivity.Current` or inject `IConnectivity`.
- `NetworkAccess` is one of `Internet`, `ConstrainedInternet` (captive portal), `Local`, `None` or
  `Unknown`. `ConnectionProfiles` lists the active profiles (WiFi, Cellular, and so on).
- The `ConnectivityChanged` event fires when access or profiles change.
- Android needs the `ACCESS_NETWORK_STATE` permission.
- The docs warn that `Internet` can be reported when the router has no upstream. So treat
  connectivity as a *hint that triggers a Nudge*, never as proof. The send attempt is the real test.
- In the library, define your own tiny `IConnectivity { bool IsOnline { get; } }`, as the probe does,
  and adapt the MAUI one in the head. This keeps `Shop.Client` free of MAUI references and testable
  on any machine.

---

## 4. Versions (exact, as of 2026-11-04) and telemetry

### 4.1 Package and toolchain versions

| Component | Version | Notes / source |
|---|---|---|
| .NET SDK (book) | 11.0.100-rc.3.26522.148 | local `.dotnet` |
| Workload set after `dotnet workload install maui-android` | 11.0.100-rc.3.26524.4 | VERIFIED |
| MAUI workload manifest | 11.0.0-rc.2.26505.5 (feature band 11.0.100-rc.2) | VERIFIED; there is no MAUI rc.3, and the rc.3 SDK workload set ships MAUI rc.2 |
| **Microsoft.Maui.Controls** (`$(MauiVersion)`) | **11.0.0-rc.2.26505.5** (published 2026-10-13) | VERIFIED via `dotnet msbuild -getProperty:MauiVersion` and project.assets.json. Latest stable is 10.0.120. |
| .NET for Android SDK pack | Microsoft.Android.Sdk.Linux 37.2.0-rc.2.84 (API 37 bindings; also installs 36.1.118) | VERIFIED |
| iOS / Mac Catalyst manifests | 27.0.12212-net11-rc.2 | from workload set; Xcode 26.4 or 27 per release notes |
| CommunityToolkit.Mvvm | 8.4.2 (2026-03-25) | NuGet |
| CommunityToolkit.Maui | 15.0.1 | NuGet (optional) |
| Microsoft.AspNetCore.SignalR.Client | 11.0.0-rc.3.26522.148 | NuGet |
| Microsoft.Data.Sqlite | 11.0.0-rc.3.26522.148 (→ SQLitePCLRaw.bundle_e_sqlite3 3.0.5) | VERIFIED build |
| sqlite-net-pcl | 1.11.285 | NuGet |
| OpenTelemetry / .Exporter.OpenTelemetryProtocol / .Extensions.Hosting | 1.20.1 (1.21.0-rc.1 pre) | NuGet |
| OpenTelemetry.Instrumentation.Http / .Runtime | 1.20.1 | NuGet |
| Microsoft.Extensions.Http.Resilience / ServiceDiscovery | 10.11.0 | NuGet (still on a 10.x line) |
| Aspire.Hosting.Maui | 13.6.4-preview.1.26523.11 (preview only) | NuGet |
| Aspire.Hosting.AppHost / DevTunnels | 13.6.4 | NuGet |
| Akka / Akka.Persistence / Akka.Cluster.Sharding | 1.5.73 stable; 1.6.0-beta5 (2026-11-04) | NuGet, GitHub releases |
| Akka.Hosting.Maui | 1.5.8 (net6.0, stale) | NuGet; see #8790 |
| Proto.Actor | 1.8.0 | NuGet |
| Microsoft.Orleans.Client | 10.4.2 | NuGet |
| xunit.v3 | 4.0.2 | VERIFIED; needs MTP, see §5.4 |
| Shouldly | 4.3.0 (5.0.0-preview.2 pre) | VERIFIED |

Template gotchas (VERIFIED):

- `dotnet new maui` on .NET 11 RC3 emits `Microsoft.Extensions.Logging.Debug Version="11.0.0-*"`,
  a floating version. Pin it, for example to `11.0.0-rc.3.26522.148`, for reproducible tags.
- The template sets `SupportedOSPlatformVersion` for Android to 24.0 and for Mac Catalyst to 17.0.
- On Linux the template's only target framework is `net11.0-android`. The iOS and Mac Catalyst
  frameworks are added only on non-Linux hosts, and Windows only on Windows hosts.
- `dotnet new maui-aspire-servicedefaults` on the same SDK emits **stale** package versions:
  Http.Resilience and ServiceDiscovery 9.0.0, OpenTelemetry 1.15.x. Bump them to the versions above.

### 4.2 OpenTelemetry from the phone

- The MAUI service-defaults template wires up:
  - `.AddHttpClientInstrumentation()` and `.AddRuntimeInstrumentation()` for metrics
  - `AddSource(builder.Environment.ApplicationName)` and `.AddHttpClientInstrumentation()` for traces
  - `UseOtlpExporter()` only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set
  - commented-out `AddMeter("Microsoft.Maui")` and `AddSource("Microsoft.Maui")` lines
- **Getting env vars onto a phone:** a phone app has no process environment from your dev box. The
  Aspire.Hosting.Maui README (inside the 13.6.4-preview nupkg) says `WithOtlpDevTunnel()`:
  - creates a dev tunnel for the dashboard's OTLP endpoint
  - sets `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL` (prefers `http/protobuf`),
    `OTEL_SERVICE_NAME` and `OTEL_RESOURCE_ATTRIBUTES`
  - notes that "Android and iOS environment targets are reevaluated when their resource restarts"
  - requires the Aspire.Hosting.DevTunnels package and a logged-in devtunnel CLI

  So the values are baked in at build time through generated MSBuild targets. Exactly how they reach
  `IConfiguration` at runtime is **UNVERIFIED** here. Windows and Mac Catalyst targets reach
  localhost directly.

- **AppHost shape** (https://learn.microsoft.com/dotnet/maui/data-cloud/aspire-integration, preview
  feature):

  ```csharp
  var api = builder.AddProject<Projects.Shop_Api>("api");
  var tunnel = builder.AddDevTunnel("devtunnel-public").WithAnonymousAccess().WithReference(api.GetEndpoint("https"));
  var mobile = builder.AddMauiProject("mobile", @"../src/Shop.Mobile/Shop.Mobile.csproj");  // NO ProjectReference to the MAUI head
  mobile.AddAndroidEmulator().WithOtlpDevTunnel().WithReference(api, tunnel);
  ```

  Then in the app: `builder.AddServiceDefaults();` and
  `AddHttpClient<ShopApi>(c => c.BaseAddress = new("https+http://api"))`. The MAUI targets do not
  launch automatically; start each one from the dashboard.

  Aspire requirements for the book: `AddMauiProject` points at a csproj path, and the docs say *not*
  to add a ProjectReference from the AppHost (incompatible target frameworks). The AppHost therefore
  builds fine on machines without MAUI. But if the book's AppHost always calls `AddMauiProject`, the
  dashboard resource will fail to start on a machine without the workload. Guard it with a
  configuration flag such as `if (builder.Configuration["IncludeMobile"] == "true")`.
  **UNVERIFIED:** whether `AddMauiProject` itself fails at AppHost startup when the workload is
  missing.

- **Trace correlation phone → API → actor:**
  - **HTTP:** `OpenTelemetry.Instrumentation.Http` creates client spans and the default propagator
    injects `traceparent`. ASP.NET Core extracts it. From there the book's existing telemetry chapter
    carries the context through the Ask/Tell envelope into the actor and the `Persist` span.
  - **SignalR hub invocations (VERIFIED in source):** the .NET client has an `ActivitySource` named
    `Microsoft.AspNetCore.SignalR.Client`. Each `InvokeAsync`/stream creates a Client-kind activity
    `Microsoft.AspNetCore.SignalR.Client.InvocationOut` tagged `rpc.system=signalr`. `InjectHeaders`
    calls `DistributedContextPropagator.Current.Inject(...)` into `HubInvocationMessage.Headers`
    (dotnet/aspnetcore `src/SignalR/clients/csharp/Client.Core/src/HubConnection.cs`). This is
    available since .NET 9 (https://github.com/dotnet/aspnetcore/pull/57101); add
    `.AddSource("Microsoft.AspNetCore.SignalR.Client")` on the phone.
  - **Server → client pushes** do *not* parent client-side work automatically. If the chapter wants
    "projection updated → phone redrew" in one trace, put the `traceparent` of the projection span in
    the `OrderSummaryChanged` DTO and start a client activity with that parent. That is a nice
    teaching moment, but optional.
  - **Outbox retries:** start the client span when the user taps, store its `traceparent` in the
    outbox row, and start each send attempt as a child, or a link if the attempt happens after a
    restart. Then a command sent three hours later, after the phone came back online, still traces
    back to the original tap. **Design only, UNVERIFIED.**
- **Privacy and cost:** never put user input in span attributes. Sample aggressively in production
  (parent-based, low ratio) because phones are many. The OTLP endpoint for production phones must be
  a public, authenticated collector (an OpenTelemetry Collector or a vendor), never the Aspire
  dashboard. Exporting from cellular costs the user's data and battery, so batch and export on Wi-Fi
  or while foregrounded.

---

## 5. Build feasibility on this machine

### 5.1 What exists

- **Host:** `jfw16f`. `~/code/ops/jfw16f/README.md` has no Android or JDK notes.
- **Android SDK: not installed.**
  - `$ANDROID_HOME`, `$ANDROID_SDK_ROOT`, `~/Android/Sdk`, `/opt/android*` and `/usr/lib/android-sdk`
    are all absent. There is no `adb` or `sdkmanager` on PATH and no `platform-tools` anywhere under `/`.
  - `~/.android/` exists with an AVD definition, `pixel_play_36.avd` (target android-36,
    `system-images/android-36/google_apis_playstore/x86_64/`), plus adb keys dated 2026-10-06. So an
    SDK existed or was used here once, but it is gone now.
  - Rider's settings point at a Windows path (`C:\Program Files (x86)\Android\android-sdk`).
- **JDK:**
  - Only `java-25-openjdk-headless` is installed: a runtime, not a JDK.
  - .NET for Android warns: "Could not find required file `jar` within `/usr/lib/jvm/java-25-openjdk`;
    is this a valid JDK?"
  - Fix with `sudo dnf install java-25-openjdk-devel`. The RC2 release notes mention JDK 25 support
    for Android.
  - Alternatively, install Microsoft OpenJDK through the InstallAndroidDependencies target below.

### 5.2 What I ran

1. `dotnet workload install maui-android` into the local SDK. **Worked in about 18 s.** Results:
   - workload `maui-android` 11.0.0-rc.2.26505.5/11.0.100-rc.2
   - packs: Microsoft.Android.Sdk.Linux 37.2.0-rc.2.84 and 36.1.118, Microsoft.Maui.Sdk
     11.0.0-rc.2.26505.5, Android CoreCLR/Mono/NativeAOT runtime packs
   - only `/home/jonathan/code/ddd-actors-dotnet/.dotnet` was touched
   - `dotnet workload search` also lists `android`, `maui-windows`, `maui-tizen` and `macos`, but
     **no `maui-ios`/`maui-maccatalyst` on Linux**
2. `dotnet new maui -n Probe.Mobile` worked. `dotnet build -f net11.0-android` **failed**:
   `XA5300: The Android SDK directory could not be found` (plus the JDK warning above).
3. `dotnet build -t:InstallAndroidDependencies -f net11.0-android -p:AndroidSdkDirectory=<scratch>/android-sdk
   -p:JavaSdkDirectory=<scratch>/jdk -p:AcceptAndroidSDKLicenses=True` would download the Android SDK
   and a JDK and accept Google's SDK licences. **The sandbox permission policy blocked this**, so it
   was not run. It is the documented way to provision a headless Linux build box
   (https://aka.ms/dotnet-android-install-sdk). The author can run it, then build with
   `-p:AndroidSdkDirectory=…` or set `ANDROID_HOME`.

**Net result:** the MAUI workload installs and MAUI projects restore and evaluate on this machine with
the book SDK. An APK cannot be built until an Android SDK and a full JDK are installed (§5.1). I did
not get an Android build.

### 5.3 Repo structure that keeps every tag buildable without MAUI (VERIFIED)

```
Shop.slnx                      <- the ONLY solution at the repo root; no MAUI projects
global.json                    <- pins SDK; "test": { "runner": "Microsoft.Testing.Platform" }
Directory.Build.props
src/
  Shop.Contracts/              net11.0, IsAotCompatible, JsonSerializerContext
  Shop.Client/                 net11.0, IsAotCompatible: view models, outbox, dispatcher, hub client
  Shop.Mobile/                 net11.0-android (+ios/maccatalyst off-Linux), UseMaui; thin head:
                               MauiProgram, pages/XAML, adapters (IConnectivity, dispatcher, DB path)
  ...server projects...
tests/
  Shop.Client.Tests/           net11.0, xunit.v3: runs everywhere, in the default solution
mobile/
  Shop.Mobile.slnx             <- Contracts + Client + Mobile + Client.Tests (paths ../src/...)
```

Results:

- `dotnet build` at the root with only `Shop.slnx` there builds without MAUI. Keep the mobile
  solution in a *subfolder*. Two solution files at the root make a bare `dotnet build` fail with
  MSB1011 ("Specify which project or solution file to use because this folder contains more than one
  project or solution file"). VERIFIED.
- **Simulating a machine without workloads:** `MSBuildEnableWorkloadResolver=false dotnet build
  Shop.slnx` gives **Build succeeded**. The same against `mobile/Shop.Mobile.slnx` fails with
  `NETSDK1208: The target platform identifier android was not recognized`, which is exactly what a
  reader without MAUI would see.
- `dotnet test --solution Shop.slnx` gives **10/10 passed**: 8 client tests and 2 Akka dedupe tests.
- `dotnet build mobile/Shop.Mobile.slnx` on this machine (workload present, no SDK) fails with
  XA5300, as expected.
- A `.slnf` filter is the wrong tool here. A filter can only *subtract* from a parent solution, so the
  MAUI head would have to sit in the root solution, and then a plain `dotnet build` would include it.
  A conditional `<TargetFrameworks>` hack is fragile, because detecting an installed workload from
  MSBuild has no supported property. Two solutions is the simplest robust option.
- CI: the default job runs `dotnet test --solution Shop.slnx` on ubuntu. An optional job runs
  `dotnet workload install maui-android` and `-t:InstallAndroidDependencies`, then builds
  `mobile/Shop.Mobile.slnx`.

### 5.4 Test-runner note (VERIFIED, relevant to every chapter)

xunit.v3 **4.0.2** runs on Microsoft.Testing.Platform. With the .NET 11 SDK, `dotnet test` fails:

> Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later

unless `global.json` contains `"test": { "runner": "Microsoft.Testing.Platform" }`. With MTP you do
not need `Microsoft.NET.Test.Sdk` or `xunit.runner.visualstudio`. The xunit.v3 analyzers also raise
**xUnit1051** (pass `TestContext.Current.CancellationToken`). Under `TreatWarningsAsErrors` that
breaks the build, so the sample code threads the token through.

---

## 6. Proposed chapter

**Title:** "The Mobile Edge: Offline-First Clients for an Actor-Based Domain". An alternative is
"DDD at the Edge: What Belongs on the Phone".

**Length:** about 7,500 words, in 9 sections.

**Placement:** after the API/integration chapter and the CQRS projections chapter, and **after the
telemetry chapter**. Section 8 can then reuse the server's tracing setup and show one trace from a tap
on the phone to the journal write. If the telemetry chapter comes later in the book, move §8 into that
chapter as a "clients" section. In a part structure, this chapter belongs in "Edges of the system"
alongside any integration and anti-corruption chapter, near the end of the book. Readers need
idempotent commands, projections and SignalR/SSE first.

**Assumed prior state:** pure C# domain core; Akka.NET persistent aggregates behind cluster sharding;
ASP.NET Core API; CQRS projections; OpenTelemetry; Aspire AppHost.

| # | Section (≈words) | Code change? | What changes |
|---|---|---|---|
| 1 | **Where the domain lives** (800) | none | The question, the answer, context map (OHS/PL, customer/supplier, client as its own bounded context). The five reasons not to ship aggregates to the device. What the device *does* own. |
| 2 | **A Published Language for the client** (900) | **yes** | Extract `Shop.Contracts` (net11.0, `IsAotCompatible`). Commands gain `Guid CommandId`; replies `CommandAccepted`/`CommandRejected(Code, Reason)`; notification DTO `OrderSummaryChanged(Version…)`; source-generated `JsonSerializerContext`. API endpoints map Accepted/Rejected/timeout to 200, 409/422 ProblemDetails, and 503/504. |
| 3 | **Commands you can send twice** (1,100) | **yes (server)** | Events carry `CommandId`; the decider returns Duplicate; the aggregate replies to duplicates with the original answer; bounded dedupe window; why single-writer sharding makes it race-free. Tests: duplicate, then restart and recover, then duplicate again, giving one event. Optional `ExpectedVersion` and `stale` rejection for non-commutative commands. |
| 4 | **Client logic as a plain library** (1,000) | **yes (new projects)** | `Shop.Client` (net11.0) plus `Shop.Client.Tests`. `Quantity` value object; `union CommandStatus`; CommunityToolkit.Mvvm view model; client-generated `Guid.CreateVersion7()` ids; the ubiquitous language in UI strings. Explain why the library never references MAUI. |
| 5 | **Offline first: an outbox and a mailbox** (1,200) | **yes** | `SqliteOutbox` (Microsoft.Data.Sqlite, WAL, `INSERT OR IGNORE`). `OutboxDispatcher` as a Channel single-reader loop: the actor shape without a framework. `SendOutcome` union; transient vs rejected; ordering; backoff; max age. Tests for restart survival, retry with the same id, terminal rejection and double tap. |
| 6 | **Read models on the device** (1,100) | **yes (both sides)** | Projection publishes `OrderSummaryChanged` to a SignalR group. Client `HubConnection` with `WithAutomaticReconnect`, an initial-start retry loop, and catch-up `GET` on `Reconnected` and `Resumed`. SQLite cache with version-guarded upsert; optimistic overlay merge; conflict UX for rejections and `stale`. |
| 7 | **Should the phone run actors?** (800) | none (or one sidebar listing) | Evidence table from §2.2 (trim/AOT results, Akka.Hosting vs MAUI hosted services, lifecycle and battery, size). Verdict: Channels on phones; Akka 1.6 as an option for kiosk, desktop and edge; Orleans client never on device. |
| 8 | **One trace from tap to journal** (700) | **yes** | MAUI service defaults (bumped packages); `AddSource("Microsoft.AspNetCore.SignalR.Client")`; `traceparent` stored with outbox rows; AppHost `AddMauiProject(...).AddAndroidEmulator().WithOtlpDevTunnel()` behind an `IncludeMobile` flag. Screenshot of the Aspire trace (requires an Android SDK machine). |
| 9 | **The thin MAUI head and keeping the build green** (500) | **yes** | `src/Shop.Mobile` (MAUI template, pinned versions, `FileSystem.AppDataDirectory` DB path, `IConnectivity` adapter, lifecycle hooks `Stopped` → stop hub and dispatcher, `Resumed` → restart, catch up, Nudge). `mobile/Shop.Mobile.slnx`; CI split; the `MSBuildEnableWorkloadResolver=false` check. |

**Sample-repo tag plan:**

1. `ch-mobile-01-contracts`: §2 and §3, server only. Builds everywhere.
2. `ch-mobile-02-client`: §4 to §6. Library and tests only. Builds everywhere.
3. `ch-mobile-03-head`: §8 and §9. Adds `src/Shop.Mobile` and `mobile/Shop.Mobile.slnx`. The default
   solution is unaffected.

**Risks the editor should know about:**

- **No device run yet.** Nothing has been run on Android or iOS. Before §8's screenshots and any claim
  that the app works on a phone, someone with an Android SDK must run `InstallAndroidDependencies` or
  install Android Studio's SDK.
- **iOS needs a Mac with Xcode 26.4 or 27.** It can't be built on this Linux box.
- **Versions will move.** MAUI is still at rc.2 under the rc.3 SDK. .NET 11 GA is planned for November
  2026, so re-pin every version in §4.1 at GA.
- **Akka is moving too.** Akka.NET 1.6 is in beta (beta5 is dated 2026-11-04), and the #8790
  `AddAkkaMaui` work is unmerged. Re-check §7's evidence before print.

---

## 7. Sources

- What's new in .NET MAUI for .NET 11: https://learn.microsoft.com/dotnet/maui/whats-new/dotnet-11
- Trim a .NET MAUI app: https://learn.microsoft.com/dotnet/maui/deployment/trimming
- App lifecycle: https://learn.microsoft.com/dotnet/maui/fundamentals/app-lifecycle
- Connectivity: https://learn.microsoft.com/dotnet/maui/platform-integration/communication/networking
- Local databases: https://learn.microsoft.com/dotnet/maui/data-cloud/database-sqlite
- .NET MAUI and Aspire: https://learn.microsoft.com/dotnet/maui/data-cloud/aspire-integration and https://aspire.dev/integrations/dotnet/maui
- SignalR .NET client: https://learn.microsoft.com/aspnet/core/signalr/dotnet-client
- SignalR client tracing: https://github.com/dotnet/aspnetcore/pull/57101 and `src/SignalR/clients/csharp/Client.Core/src/HubConnection.cs` (main)
- What's new in C# 15: https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-15
- Akka.NET issues:
  - https://github.com/akkadotnet/akka.net/issues/7246 (AOT epic)
  - #5153 (Xamarin config failure)
  - #6459 (Android `PlatformNotSupportedException`, fixed)
  - #8790 (Akka.Hosting.Maui for 1.6, draft PR)
  - #8794 and #8795 (MAUI detection in Akka.Hosting)
  - #8605 (AOT Props parser, merged)
- MAUI hosted services: https://github.com/dotnet/maui/issues/2244
- Android SDK install for .NET: https://aka.ms/dotnet-android-install-sdk
- NuGet flat-container indexes for every version in §4.1: `https://api.nuget.org/v3-flatcontainer/<id>/index.json`, queried 2026-11-04
