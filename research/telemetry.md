# Research notes: Instrumentation & Telemetry for Akka.NET on .NET 11

*Researched 2026-11-04 (local SDK 11.0.100-rc.3.26522.148). Audience: writers of the "Instrumentation & Telemetry" and "Operations" chapters.*

**Verified sample:** `/home/jonathan/code/ddd-actors-dotnet/research/telemetry-sample/` (`OtelAkka.csproj`, `net11.0`). It builds with zero warnings. `dotnet run -- --selftest` runs an in-process end-to-end check with in-memory exporters, and the output quoted below comes from that run. It was also exported over OTLP/gRPC to an `otel/opentelemetry-collector-contrib:0.142.0` container, which received 11 spans, 28 metrics and 8 log records. Unless a passage says otherwise, every code block here is taken from that project and compiles.

Legend: **VERIFIED** = compiled and/or ran locally. **SOURCE** = read in primary docs or a package. **UNVERIFIED** = not confirmed; treat with care.

---

## 1. Exact versions (NuGet flat-container, queried 2026-11-04)

| Package | Latest stable | Latest pre-release | Notes |
|---|---|---|---|
| `OpenTelemetry` / `OpenTelemetry.Api` | **1.20.1** | 1.21.0-rc.1 | core SDK |
| `OpenTelemetry.Extensions.Hosting` | **1.20.1** | 1.21.0-rc.1 | `AddOpenTelemetry()` |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | **1.20.1** | 1.21.0-rc.1 | `UseOtlpExporter()` |
| `OpenTelemetry.Exporter.Console` | 1.20.1 | | dev only |
| `OpenTelemetry.Exporter.InMemory` | 1.20.1 | | tests |
| `OpenTelemetry.Instrumentation.AspNetCore` | **1.20.1** | | stable |
| `OpenTelemetry.Instrumentation.Http` | **1.20.1** | | stable |
| `OpenTelemetry.Instrumentation.Runtime` | **1.20.1** | | stable |
| `OpenTelemetry.Instrumentation.Process` | (none) | 1.20.1-rc.1 | |
| `OpenTelemetry.Exporter.Prometheus.AspNetCore` | **(none: still beta)** | 1.20.1-beta.1 | see §9 |
| `Akka`, `Akka.Hosting`, `Akka.Cluster.Hosting`, `Akka.Persistence.Hosting`, `Akka.Cluster.Sharding`, `Akka.Management`, `Akka.Persistence.Sql.Hosting`, `Akka.Hosting.TestKit`, `Akka.TestKit.Xunit2` | **1.5.73** | 1.6.0-beta5 | 1.6 ships Hosting at the same version |
| `Akka.HealthCheck*` | 1.5.37 | | **deprecated**, do not use (see §8) |
| `Akka.Logger.Serilog` | 1.5.69 | | |
| `Akka.Logger.Extensions.Logging` | 1.4.22 | | **obsolete**; MEL logging is built into Akka.Hosting |
| `Petabridge.Cmd.Host` / `.Cluster` / `.Cluster.Sharding` / `.Remote` | **1.5.2** | | 1.5.2 released 2026-10-04 |
| `Phobos.*` | not on nuget.org | | commercial, see §5 |
| `Aspire.Hosting.AppHost`, `Aspire.AppHost.Sdk`, `Aspire.Hosting.Testing` | **13.6.4** | | Aspire CLI installed locally: 13.6.3 |
| `Microsoft.Extensions.ServiceDiscovery`, `Microsoft.Extensions.Http.Resilience` | 10.11.0 | | used by the ServiceDefaults template |
| `Microsoft.Extensions.Diagnostics.Testing` | 10.11.0 | | `MetricCollector<T>` |
| `Serilog.Sinks.OpenTelemetry` | 4.2.0 | | |
| `Azure.Monitor.OpenTelemetry.AspNetCore` | 1.6.0 | | |

Container images, from the registry tag list:
- `mcr.microsoft.com/dotnet/aspire-dashboard:13.6.2`. aspire.dev now documents the image as `mcr.microsoft.com/aspire/dashboard:latest`, which also carries `13.6.x` tags.
- `docker.io/otel/opentelemetry-collector-contrib:0.142.0`
- `docker.io/grafana/otel-lgtm:0.39.0`

The registry listing may be truncated, so re-check these tags before printing them in the book.

Version facts from the Akka.NET release notes (https://github.com/akkadotnet/akka.net/releases):
- 1.5.73 was released 2026-10-05. 1.6.0-beta5 was released 2026-11-04.
- 1.6 **targets .NET 10 only**, so it runs on .NET 11 but excludes .NET Framework, .NET 6 and .NET 8.
- Akka.Hosting moved into the main akka.net repo for 1.6 and now ships at the same version. Package IDs and namespaces are unchanged, and the old Akka.Hosting repo stops at 1.5.71.
- **Akka.Hosting 1.5.73 depends on `OpenTelemetry >= 1.12.0` and `OpenTelemetry.Api >= 1.15.3`** (SOURCE: the nuspec). Those floors exist because of advisory GHSA-g94r-2vxg-569j. Pinning OTel 1.20.1 satisfies them.

Gotcha: the Aspire 13.6.4 `aspire-servicedefaults` template still generates `net10.0` and **OpenTelemetry 1.17.0** packages (VERIFIED by generating it). Tell readers to bump the project to `net11.0` and the packages to 1.20.1.

---

## 2. The .NET building blocks: `ActivitySource`, `Meter`, `ILogger`

OpenTelemetry .NET does not ship its own tracing or metrics API. It **is** the BCL API, plus an SDK that listens to it:
- **Traces:** `System.Diagnostics.ActivitySource` → `Activity`. An Activity is an OTel span.
- **Metrics:** `System.Diagnostics.Metrics.Meter` → `Counter<T>`, `Histogram<T>`, `UpDownCounter<T>`, `Gauge<T>`, `ObservableGauge<T>`, and so on.
- **Logs:** `Microsoft.Extensions.Logging.ILogger`, bridged by `builder.Logging.AddOpenTelemetry(...)`.

Domain code needs only the BCL types, which keeps the domain layer free of OTel SDK references. That is a good DDD story: telemetry is a port, and the SDK is an adapter wired in the composition root.

Rules worth teaching:
- Use **one `ActivitySource` and one `Meter` per bounded context**, named like `Bookstore.Ordering`. The SDK only collects what you register with `AddSource(name)` / `AddMeter(name)`. **Forgetting `AddSource`/`AddMeter` is the #1 "my spans don't show up" bug.**
- `StartActivity` returns **`null` when nothing listens**. Always write `activity?.SetTag(...)`. This is also why instrumentation is nearly free when it is disabled.
- Create meters through `IMeterFactory` (from DI; `AddMetrics()` is already registered by the generic host). That way tests can isolate instruments with `MetricCollector<T>`. A static `ActivitySource` is still the norm.
- **New in .NET 11 (SOURCE + VERIFIED):** `services.AddTracing(b => b.EnableTracing(...)/DisableTracing(...))` in `Microsoft.Extensions.Diagnostics`, plus `System.Diagnostics.ActivitySourceFactory`. `ActivitySource` is now **unsealed**. With these you get DI-created sources and rule- or configuration-based enabling of sources, the way logging filters work. VERIFIED: `AddTracing()` registers `DefaultActivitySourceFactory`, whose `Create(new ActivitySourceOptions("Bookstore.Ordering"){Version="1.0.0"})` returns a `FactoryActivitySource`. UNVERIFIED: how this interacts with the OTel SDK's `AddSource`. My recommendation is to stay with a static `ActivitySource` plus `AddSource` in the book and mention `AddTracing` as a sidebar. (https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries)
- Also new in .NET 11: the `LoggerMessage` source generator supports generic methods, and MemoryCache emits `dotnet.cache.*` metrics when `TrackStatistics = true`.
- Units follow UCUM: durations in **seconds (`"s"`)** as `double`, and counts as `"{command}"`-style annotations. Instrument names use dotted lowercase, for example `ordering.command.duration`.
- `InstrumentAdvice<T>.HistogramBucketBoundaries` lets the instrument suggest buckets. The default OTel buckets are designed for milliseconds-scale values in some exporters, so actor latencies of 0.1–5 ms need explicit small buckets (VERIFIED: it compiles and records).

Verified instrument set (`Telemetry.cs`):

```csharp
public static class OrderingDiagnostics
{
    public const string SourceName = "Bookstore.Ordering";   // used for both ActivitySource and Meter
    public static readonly ActivitySource Source = new(SourceName, "1.0.0");
}

public sealed class OrderingMetrics
{
    public Counter<long> CommandsProcessed { get; }
    public Histogram<double> CommandDuration { get; }
    public Histogram<double> PersistDuration { get; }
    public Counter<long> CommandsRejected { get; }
    public UpDownCounter<long> ActiveAggregates { get; }

    public OrderingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(OrderingDiagnostics.SourceName, "1.0.0");
        CommandsProcessed = meter.CreateCounter<long>("ordering.commands.processed", unit: "{command}",
            description: "Commands handled by Order aggregates");
        CommandsRejected = meter.CreateCounter<long>("ordering.commands.rejected", unit: "{command}",
            description: "Commands rejected by invariants");
        CommandDuration = meter.CreateHistogram<double>("ordering.command.duration", unit: "s",
            description: "Time from dequeue to handler completion",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.0005, 0.001, 0.0025, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1] });
        PersistDuration = meter.CreateHistogram<double>("ordering.persist.duration", unit: "s",
            description: "Time from Persist() to the persist callback");
        ActiveAggregates = meter.CreateUpDownCounter<long>("ordering.aggregates.active", unit: "{actor}",
            description: "Order aggregate actors currently alive");
    }
}
```

Unit-testing metrics (VERIFIED, `Probes.cs`, package `Microsoft.Extensions.Diagnostics.Testing` 10.11.0):

```csharp
var services = new ServiceCollection().AddMetrics().AddSingleton<OrderingMetrics>().BuildServiceProvider();
var metrics = services.GetRequiredService<OrderingMetrics>();
var meterFactory = services.GetRequiredService<IMeterFactory>();
using var collector = new MetricCollector<long>(meterFactory, OrderingDiagnostics.SourceName, "ordering.commands.processed");
metrics.CommandsProcessed.Add(1, new KeyValuePair<string, object?>("command.type", "PlaceOrder"));
var last = collector.GetMeasurementSnapshot().Single();   // last.Value == 1, last.Tags["command.type"] == "PlaceOrder"
```

To test traces, register an `ActivityListener` in the test, or use `OpenTelemetry.Exporter.InMemory` with `AddInMemoryExporter(List<Activity>)`, which is what the self-test does.

---

## 3. Wiring OpenTelemetry in an Akka.Hosting app (VERIFIED)

From `Program.cs` (in-memory exporters shown for the self-test only):

```csharp
builder.Logging.AddOpenTelemetry(o =>
{
    o.IncludeFormattedMessage = true;
    o.IncludeScopes = true;
    o.AddAkkaTraceCorrelation();          // Akka.Hosting >= 1.5.59: must come BEFORE exporters
});

builder.Services.AddSingleton<OrderingMetrics>();

var otel = builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName: "ordering-api", serviceVersion: "1.2.3")
        .AddAttributes([new("deployment.environment.name", builder.Environment.EnvironmentName)]))
    .WithTracing(t => t
        .AddSource(OrderingDiagnostics.SourceName)
        .AddAspNetCoreInstrumentation(o => o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/healthz"))
        .AddHttpClientInstrumentation())
    .WithMetrics(m => m
        .AddMeter(OrderingDiagnostics.SourceName, "Akka.Lifecycle", "Akka.Mailbox")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation());

// OTLP only when an endpoint is configured (Aspire sets OTEL_EXPORTER_OTLP_ENDPOINT for you)
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
    otel.UseOtlpExporter();

builder.Services.AddHealthChecks();
builder.Services.AddAkka("ordering", (akka, sp) =>
{
    akka.AddHocon("akka.actor.telemetry.enabled = on", HoconAddMode.Prepend)   // ActorStarted/Stopped/Restarted events
        .ConfigureLoggers(setup =>
        {
            setup.LogLevel = Akka.Event.LogLevel.InfoLevel;
            setup.ClearLoggers();
            setup.AddLoggerFactory();     // Akka logs -> Microsoft.Extensions.Logging -> OTel
        })
        .WithActorSystemLivenessCheck(tags: ["live", "akka"])
        .AddPetabridgeCmd(cmd => cmd.RegisterCommandPalette(ClusterCommands.Instance))
        .WithActors((system, registry, resolver) =>
        {
            system.ActorOf(Props.Create(() => new ActorLifecycleMetrics()), "actor-lifecycle-metrics");
            var order = system.ActorOf(resolver.Props<OrderActor>("o-1"), "order-o-1");
            registry.Register<OrderActor>(order);
        });
});
```

Notes:
- `UseOtlpExporter()` is the cross-signal helper (traces, metrics and logs in one call). It reads the standard env vars: `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL` (`grpc` | `http/protobuf`), `OTEL_SERVICE_NAME`, `OTEL_RESOURCE_ATTRIBUTES`, `OTEL_METRIC_EXPORT_INTERVAL`, and others. It cannot be combined with the per-signal `AddOtlpExporter()` calls on the same provider. VERIFIED end-to-end against the collector with `OTEL_EXPORTER_OTLP_ENDPOINT=http://127.0.0.1:14317`.
- `AddRuntimeInstrumentation()` is now parameterless. The options overload was obsoleted in 1.20.0. On .NET 9+ the runtime emits `System.Runtime` meter metrics natively, and the package forwards those. (SOURCE: Instrumentation.Runtime 1.20.0 notes via https://newreleases.io/project/github/open-telemetry/opentelemetry-dotnet-contrib/release/Instrumentation.Runtime-1.20.0)
- OTel 1.20.0 highlights (SOURCE: https://github.com/open-telemetry/opentelemetry-dotnet/releases):
  - `TracerProviderBuilder.ConfigureSampler`
  - a fix for `SuppressInstrumentationScope` leaking across async contexts
  - the OTLP exporter falls back to a default `HttpClient` when the configured one can't be used
- Resource attribute `deployment.environment.name` is the current semconv name. `deployment.environment` is deprecated.
- Put `serviceVersion` from your assembly informational version or MinVer (see the Operations chapter). It is what lets you compare a canary with stable in any backend.

---

## 4. Trace context does NOT cross the mailbox: propagate it yourself

### 4.1 Facts
- **VERIFIED:** with a plain `Tell`/`Ask` made inside an active span, `Activity.Current` inside the actor's handler is **`null`**. The self-test prints `Activity.Current in actor, plain Tell = <null>`. Two things cause this. `Activity.Current` is `AsyncLocal`-based. And the dispatcher runs the mailbox on a thread-pool work item that does not capture the sender's `ExecutionContext`; even if it did, one mailbox run processes messages from many senders.
- Akka.Hosting's own XML docs say the same: "`Activity.Current` doesn't flow across actor mailbox boundaries because it uses `AsyncLocal<T>`" (SOURCE: `AkkaTraceContextProcessor` docs in Akka.Hosting 1.5.73).
- **Akka.NET core (1.5.73 and 1.6.0-beta1…beta5) has no built-in ActivitySource or Meter for actor messaging.** I read all 1.6 beta release notes. The only telemetry-adjacent items are:
  - the `LoggerSetup` type (beta1)
  - a logger-name resolution fix (beta2/3)
  - "generator diagnostics" (beta4), which are Roslyn analyzer diagnostics for the new serializer source generator, not runtime telemetry.
- What core *does* have:
  1. **Actor lifecycle telemetry events** (since 1.4.47): `akka.actor.telemetry.enabled = on` publishes `ActorStarted` / `ActorStopped` / `ActorRestarted`, all `IActorTelemetryEvent`, to the EventStream (§6.3).
  2. **Log/trace correlation** in Akka.Hosting's MEL logger (§7).
- **Automatic** cross-actor propagation (Tell, Ask, PipeTo, Streams, Remoting, Sharding) is still the job of the commercial **Phobos** (§5).

### 4.2 The pattern: a W3C trace envelope (VERIFIED)
Carry **strings** (`traceparent`, `tracestate`), not `ActivityContext`. Strings serialize across Akka.Remote and Cluster.Sharding unchanged, and they are the W3C wire format.

```csharp
public sealed record TraceEnvelope(object Message, string TraceParent, string? TraceState)
{
    public static object Wrap(object message)
    {
        var current = Activity.Current;
        return current is null ? message : new TraceEnvelope(message, current.Id!, current.TraceStateString);
    }

    public ActivityContext ParentContext =>
        ActivityContext.TryParse(TraceParent, TraceState, isRemote: true, out var ctx) ? ctx : default;
}

public static class TracedTellExtensions
{
    public static void TellTraced(this IActorRef target, object message, IActorRef? sender = null) =>
        target.Tell(TraceEnvelope.Wrap(message), sender ?? ActorRefs.NoSender);

    public static Task<T> AskTraced<T>(this IActorRef target, object message, TimeSpan timeout) =>
        target.Ask<T>(TraceEnvelope.Wrap(message), timeout);

    /// Unwraps C# unions (System.Runtime.CompilerServices.IUnion) so spans name the case, not the union.
    public static string MessageName(object message) => message switch
    {
        IUnion u when u.Value is not null => u.Value.GetType().Name,
        _ => message.GetType().Name
    };
}
```

- `Activity.Id` is the W3C `traceparent` string when the default ID format is W3C, which it is in .NET 5+.
- Wrap only when there is a current activity, so untraced traffic pays nothing.
- If you also need **Baggage**, use `Propagators.DefaultTextMapPropagator.Inject/Extract` (from `OpenTelemetry.Api`) into a `Dictionary<string,string>` header bag instead of two fixed strings. That is UNVERIFIED in the sample but is a standard API.
- C# union note (VERIFIED on this SDK): a `union` declaration compiles to a **struct** that implements `System.Runtime.CompilerServices.IUnion` (property `object Value`) and carries `[Union]`. Telling a union to an actor **boxes** it. `Receive<OrderCommand>` matches the boxed union, and patterns like `cmd switch { PlaceOrder p => … }` match on the case type. A non-exhaustive switch is **warning CS8509**, not an error, so add `<WarningsAsErrors>CS8509</WarningsAsErrors>` (done in the sample csproj).

### 4.3 Receiving side: override `AroundReceive` (VERIFIED)
`ActorBase.AroundReceive(Receive receive, object message)` is `protected virtual`. Unwrap the envelope there, start a CONSUMER span parented on the remote context, and call `base.AroundReceive` with the **inner** message, so handlers never see the envelope.

```csharp
public abstract class TracedPersistentActor : ReceivePersistentActor
{
    protected readonly OrderingMetrics Metrics;
    private readonly string _actorType;
    private bool _handlerInvoked;   // false after base.AroundReceive => Eventsourced stashed the message

    protected TracedPersistentActor(OrderingMetrics metrics) { Metrics = metrics; _actorType = GetType().Name; }

    protected override bool AroundReceive(Receive receive, object message)
    {
        if (message is not TraceEnvelope env)
            return base.AroundReceive(receive, message);

        var messageName = TracedTellExtensions.MessageName(env.Message);
        using var activity = OrderingDiagnostics.Source.StartActivity(
            $"process {_actorType}", ActivityKind.Consumer, env.ParentContext);
        activity?.SetTag("messaging.system.name", "akka");          // semconv >= 1.45 name; "akka" is not a registered value
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("messaging.operation.name", "process");
        activity?.SetTag("messaging.destination.name", _actorType); // low-cardinality: type, not path
        activity?.SetTag("akka.message.type", messageName);
        activity?.SetTag("akka.actor.path", Self.Path.ToString());   // high-cardinality: span attr only, never a metric tag

        var start = Stopwatch.GetTimestamp();
        _handlerInvoked = false;
        try
        {
            return base.AroundReceive(receive, env.Message);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            if (!_handlerInvoked)
            {
                // Persistent actors stash commands while a Persist() is in flight. The stash keeps the
                // whole TraceEnvelope, so the message comes back through here later: don't count it twice.
                activity?.SetTag("akka.message.stashed", true);
            }
            else
            {
                var tags = new TagList { { "aggregate.type", _actorType }, { "command.type", messageName } };
                Metrics.CommandDuration.Record(Stopwatch.GetElapsedTime(start).TotalSeconds, tags);
                Metrics.CommandsProcessed.Add(1, tags);
            }
        }
    }

    /// Use instead of Command<T> so the base class knows the handler really ran.
    protected void TracedCommand<T>(Action<T> handler) => Command<T>(m => { _handlerInvoked = true; handler(m); });
}
```

**Key empirical finding: the stash double-count.** I fired three commands back to back at a persistent aggregate. `Eventsourced` stashes incoming commands while a `Persist` is in flight. The stash stores the **original mailbox envelope**, which is our `TraceEnvelope`, so trace context **survives** stash/unstash (good). But `AroundReceive` runs **twice** for each stashed message. The first sample run counted `PlaceOrder=3, CancelOrder=2` for 2 and 1 real commands. The `_handlerInvoked` flag fixes it. Output after the fix:

```
POST /orders/o-1             kind=Server   trace=ROOT span=3fed2e parent=-
process OrderActor           kind=Consumer trace=ROOT span=714f0b parent=3fed2e msg=WhatIsCurrentActivity
process OrderActor           kind=Consumer trace=ROOT span=92116c parent=3fed2e msg=PlaceOrder
persist OrderActor           kind=Internal trace=ROOT span=5f7cd1 parent=92116c msg=OrderPlaced  6.96ms
process OrderActor           kind=Consumer trace=ROOT span=221423 parent=3fed2e msg=CancelOrder (STASHED)
process OrderActor           kind=Consumer trace=ROOT span=96decc parent=3fed2e msg=PlaceOrder (STASHED)
apply OrderActor             kind=Internal trace=ROOT span=0334eb parent=92116c
process OrderActor           kind=Consumer trace=ROOT span=1a9892 parent=3fed2e msg=CancelOrder
persist OrderActor           kind=Internal trace=ROOT span=927df7 parent=1a9892 msg=OrderCancelled
apply OrderActor             kind=Internal trace=ROOT span=250969 parent=1a9892
process OrderActor           kind=Consumer trace=ROOT span=93d2ec parent=3fed2e msg=PlaceOrder status=Error
ordering.commands.processed [command.type=PlaceOrder] 2   [command.type=CancelOrder] 1
```

The same caveat applies to user-level `Stash()` / `UnstashAll()` in `Become`-based state machines. A cleaner option for the book is to keep the "stashed" span, since it is honest about queueing time. Consider renaming it `stash OrderActor` (UNVERIFIED: whether `DisplayName` can be changed after start; `Activity.DisplayName` has a setter, so it should work).

### 4.4 Persist callbacks run outside the span (VERIFIED)
`Persist(evt, handler)` invokes `handler` **later**, from a journal-response message, after the `process` span has ended. In that callback `Activity.Current` is null, so logs and replies emitted there lose correlation. The pattern below records the persist span **retroactively** with an explicit `startTime` and `SetEndTime`, so nothing is left dangling in `Activity.Current`. It then opens a short `apply` child span around the user callback:

```csharp
protected void PersistTraced<TEvent>(TEvent @event, Action<TEvent> handler)
{
    var parent = Activity.Current?.Context ?? default;
    var startedAt = DateTimeOffset.UtcNow;
    var startTs = Stopwatch.GetTimestamp();
    Persist(@event, e =>
    {
        var elapsed = Stopwatch.GetElapsedTime(startTs);
        Metrics.PersistDuration.Record(elapsed.TotalSeconds, new TagList { { "aggregate.type", _actorType } });
        using (var span = OrderingDiagnostics.Source.StartActivity(
                   $"persist {_actorType}", ActivityKind.Internal, parent, startTime: startedAt))
        {
            span?.SetTag("akka.persistence.id", PersistenceId);
            span?.SetTag("event.type", TracedTellExtensions.MessageName(e!));
            span?.SetEndTime(startedAt.UtcDateTime + elapsed);
        }
        using var cb = OrderingDiagnostics.Source.StartActivity($"apply {_actorType}", ActivityKind.Internal, parent);
        handler(e);
    });
}
```

Other async gotchas to teach:
- `ReceiveAsync` / `CommandAsync`: a `using var activity` in `AroundReceive` would close **before** the async handler completes. Phobos had exactly this bug, fixed in 2.12.0. For async handlers, start the span inside the handler, or avoid async receives in aggregates altogether. That second option is the better DDD advice: keep aggregates synchronous and use `PipeTo` for IO.
- `PipeTo`: capture `Activity.Current?.Context` before the await and wrap the piped result in a `TraceEnvelope`. UNVERIFIED in the sample.
- `Context.System.Scheduler` / timers: messages scheduled by timers are new roots (or links). Do not reuse the stale context.

### 4.5 Parent vs link: what semconv says
OpenTelemetry Messaging semantic conventions (https://opentelemetry.io/docs/specs/semconv/messaging/messaging-spans/; status **Development**; latest semconv release is v1.45.0):
- Span name: `{messaging.operation.name} {destination}`, e.g. `process OrderActor`. Keep it low-cardinality: never put entity IDs or actor paths in names.
- Span kinds: create → PRODUCER; send → PRODUCER (or CLIENT if not the creation context); receive → CLIENT; **process → CONSUMER**; settle → CLIENT.
- Required attributes: `messaging.operation.name` and `messaging.system.name`. **v1.45.0 renamed `messaging.system` → `messaging.system.name`.** Conditionally required: `messaging.destination.name` and `messaging.operation.type`. Recommended: `messaging.message.id` and `messaging.message.conversation_id`, which suits a correlation or saga ID.
- **Links are the default** for consumer spans. Making the creation context the *parent* is opt-in for single-message processing. Book recommendation:
  - **Parent** for request/response command flows (HTTP → aggregate → reply). This is what readers expect to see as one waterfall.
  - **Links** for fan-out, event-stream / DistributedPubSub subscribers, projections, and anything batched. Pass `links: [new ActivityLink(env.ParentContext)]` and start a new root.

### 4.6 Cluster Sharding: route through the envelope (VERIFIED compile)
The extractor must look *through* the envelope to find the entity ID, yet deliver the envelope intact so the entity can continue the trace:

```csharp
public sealed class TracedOrderExtractor(int maxShards) : HashCodeMessageExtractor(maxShards)
{
    public override string? EntityId(object message) => message switch
    {
        TraceEnvelope { Message: var inner } => EntityId(inner),
        OrderCommand c => c switch { PlaceOrder p => p.OrderId, CancelOrder x => x.OrderId },
        ShardRegion.StartEntity s => s.EntityId,
        _ => null
    };
    public override object EntityMessage(object message) => message;   // keep the envelope
}
```

Serialization: across the wire, `TraceEnvelope(object Message, …)` needs a serializer that handles a polymorphic `object`. Akka 1.5's default JSON serializer does. With the Akka 1.6 `Akka.Serialization.V2` source generator (`[AkkaSerializable]`, `[AkkaField(n)]`), you would define the envelope with explicit fields. UNVERIFIED: how V2 handles `object` payloads. A wrapper that stores the inner message as serializer-id + manifest + bytes is the robust answer.

---

## 5. Phobos (commercial): the "buy, don't build" option

SOURCE: https://phobos.petabridge.com/articles/releases/RELEASE_NOTES.html and https://petabridge.com/platform/phobos/

- **Latest: Phobos 2.12.1 (2026-08-27)**, built on Akka.NET 1.5.71. Recent releases:
  - 2.12.0 (2026-04-29): automatic trace propagation through **Akka.Streams**, and a fix for `ReceiveAsync`/`CommandAsync` spans closing early
  - 2.11.x (March–April 2026): fixes to sharding trace continuity (`ShardRegionProxy` buffering, `StartProxyAsync`); `Activity.Current` is set during `PreStart`
  - 2.11.0: **log-trace correlation**; `AppendLogsToTrace` deprecated
- No statement found about Akka.NET 1.6 support (UNVERIFIED).
- Phobos 2.x is **built on OpenTelemetry**; 1.x used OpenTracing and App.Metrics. Packages: `Phobos.Actor`, `Phobos.Hosting`, `Phobos.Actor.Common`. They are **not on nuget.org**: the `Phobos.Actor` id there is an unrelated 0.0.1-beta. You get them via a purchase on sdkbin.com and a private feed. UNVERIFIED: exact feed mechanics and pricing. The 1.0 notes say "no per-node licensing".
- API shape (SOURCE, not compiled since no license):

  ```csharp
  akka.WithPhobos(AkkaRunMode.Local /* or Cluster */, cfg => cfg.WithTracing(t => t.SetTraceAsk(false)));
  services.AddOpenTelemetry()
      .WithTracing(t => t.AddPhobosInstrumentation())
      .WithMetrics(m => m.AddPhobosInstrumentation());
  ```

- What it gives automatically:
  - spans for every actor receive, with context propagation through Tell, Ask, PipeTo, Streams, Remoting and Sharding
  - "trace compression" noise filtering
  - metrics for messages processed by actor type / message type, mailbox depth, processing latency, error and log rates, and cluster membership
  - prebuilt Grafana dashboards (Prometheus data source) for shard and entity distribution, P50/P95/P99 actor latency, and similar
- Book stance: teach the manual envelope so readers understand the mechanism and stay vendor-neutral, then present Phobos as the production-grade alternative. Use the empirical stash finding to show why hand-rolled instrumentation has edge cases.

---

## 6. Metrics worth emitting for aggregates and actors

| Metric (suggested name) | Instrument | Tags (low cardinality only!) | How |
|---|---|---|---|
| `ordering.commands.processed` | Counter | `aggregate.type`, `command.type` | `AroundReceive` (handler actually ran) |
| `ordering.commands.rejected` | Counter | `aggregate.type`, `reason` | invariant failure path; a key **domain** signal |
| `ordering.command.duration` | Histogram (s) | `aggregate.type`, `command.type` | dequeue → handler return |
| `ordering.persist.duration` | Histogram (s) | `aggregate.type` | `Persist()` → callback (VERIFIED: in-mem journal first write ≈ 7–8 ms incl. JIT, later ≈ 0.2 ms) |
| `ordering.aggregates.active` | UpDownCounter | `aggregate.type` | `PreStart` +1 / `PostStop` −1 |
| `ordering.passivations` | Counter | `aggregate.type`, `reason` (`idle`/`explicit`/`rebalance`) | in `PostStop`, or when sending `Passivate` (UNVERIFIED: no built-in reason) |
| `akka.mailbox.depth` | ObservableGauge | `actor.type` | `ActorCell.NumberOfMessages` (below) |
| `akka.actor.live` / `akka.actor.restarts` | UpDownCounter / Counter | `actor.type` | actor telemetry events (below) |
| `akka.sharding.shards` / `akka.sharding.entities` | ObservableGauge | (`region`) | poll `GetShardRegionState` (below) |
| `akka.cluster.members` / unreachable | ObservableGauge | `status`, `role` | subscribe to `ClusterEvent.IMemberEvent` / `UnreachableMember` (UNVERIFIED in sample; standard API) |
| `akka.persistence.recovery.duration` | Histogram | `aggregate.type` | `PreStart` → `RecoveryCompleted` (UNVERIFIED in sample) |
| `akka.deadletters` | Counter | `message.type` | subscribe to `DeadLetter` on EventStream (UNVERIFIED in sample) |

Two cardinality rules:
- **Never** tag a metric with `PersistenceId`, actor path or entity ID. Those belong on spans and logs.
- Use domain event names (`OrderPlaced`), not CLR full names that include generic arguments.

### 6.1 Mailbox depth (VERIFIED compile + run)

```csharp
public sealed class MailboxDepthProbe : ReceiveActor
{
    private static readonly Meter Meter = new("Akka.Mailbox", "1.0.0");
    private static readonly ConcurrentDictionary<IActorRef, (string Type, ActorCell Cell)> Watched = new();
    private static readonly ObservableGauge<int> Gauge = Meter.CreateObservableGauge("akka.mailbox.depth",
        () => Watched.Values.GroupBy(w => w.Type)
                     .Select(g => new Measurement<int>(g.Sum(w => w.Cell.NumberOfMessages),
                                                       new KeyValuePair<string, object?>("actor.type", g.Key))),
        unit: "{message}", description: "Messages waiting in mailboxes, summed by actor type");

    public static void Track(IUntypedActorContext ctx) => Watched[ctx.Self] = (ctx.Props.Type.Name, (ActorCell)ctx);
    public static void Untrack(IActorRef self) => Watched.TryRemove(self, out _);
}
// in the aggregate: PreStart => MailboxDepthProbe.Track(Context); PostStop => MailboxDepthProbe.Untrack(Self);
```

`ICell.NumberOfMessages` is public (SOURCE: Akka.xml). Reading it from the collector thread is a racy-but-safe snapshot of the queue count; the gauge is approximate by nature. Casting `Context` to `ActorCell` is an implementation detail, so flag it in the book. The self-test prints `akka.mailbox.depth [actor.type=OrderActor] 0`. For thousands of sharded entities, sample, or track only the shard-region actors.

### 6.2 Shard stats (VERIFIED compile)

```csharp
public sealed class ShardStatsPoller : ReceiveActor, IWithTimers
{
    private static readonly Meter Meter = new("Akka.Sharding", "1.0.0");
    private static int _shards, _entities;
    private static readonly ObservableGauge<int> Shards = Meter.CreateObservableGauge("akka.sharding.shards", () => _shards, "{shard}");
    private static readonly ObservableGauge<int> Entities = Meter.CreateObservableGauge("akka.sharding.entities", () => _entities, "{entity}");
    public ITimerScheduler Timers { get; set; } = null!;

    public ShardStatsPoller(IActorRef region)
    {
        Receive<string>(s => s == "tick", _ => region.Tell(GetShardRegionState.Instance));
        Receive<CurrentShardRegionState>(s =>
        {
            _shards = s.Shards.Count;
            _entities = s.Shards.Sum(x => x.EntityIds.Count);
        });
    }
    protected override void PreStart() => Timers.StartPeriodicTimer("poll", "tick", TimeSpan.FromSeconds(10));
}
```

`GetClusterShardingStats(timeout)` gives cluster-wide counts, but use it sparingly since it fans out to all regions.

### 6.3 Actor lifecycle telemetry (VERIFIED run)
`akka.actor.telemetry.enabled = on` makes the actor system publish `ActorStarted`, `ActorStopped` and `ActorRestarted` (each an `IActorTelemetryEvent` with `Subject` and `ActorType`) on the EventStream:

```csharp
public sealed class ActorLifecycleMetrics : ReceiveActor
{
    private static readonly Meter Meter = new("Akka.Lifecycle", "1.0.0");
    private static readonly UpDownCounter<long> Live = Meter.CreateUpDownCounter<long>("akka.actor.live", "{actor}");
    private static readonly Counter<long> Restarts = Meter.CreateCounter<long>("akka.actor.restarts", "{restart}");

    public ActorLifecycleMetrics()
    {
        Receive<ActorStarted>(e => Live.Add(1, new KeyValuePair<string, object?>("actor.type", e.ActorType.Name)));
        Receive<ActorStopped>(e => Live.Add(-1, new KeyValuePair<string, object?>("actor.type", e.ActorType.Name)));
        Receive<ActorRestarted>(e => Restarts.Add(1, new KeyValuePair<string, object?>("actor.type", e.ActorType.Name)));
    }
    protected override void PreStart() => Context.System.EventStream.Subscribe(Self, typeof(IActorTelemetryEvent));
    protected override void PostStop() => Context.System.EventStream.Unsubscribe(Self);
}
```

The output includes **system actors** (`TcpManager`, `MemoryJournal`, `RecoveryPermitter`, `AkkaCmdListener` and others), so filter by namespace or by a list of domain actor types. The feature is off by default because it adds EventStream traffic on every actor start/stop; measure before enabling it on huge sharded systems. Petabridge.Cmd 1.5's `actor alive` command consumes the same events.

---

## 7. Logging: Akka → Microsoft.Extensions.Logging → OpenTelemetry

SOURCE: Akka.Hosting README and release notes (https://github.com/akkadotnet/Akka.Hosting/blob/dev/RELEASE_NOTES.md), Akka docs (https://getakka.net/articles/utilities/logging.html). VERIFIED in the sample.

- `ConfigureLoggers(setup => { setup.ClearLoggers(); setup.AddLoggerFactory(); })` routes Akka's `ILoggingAdapter` to the host's `ILoggerFactory`, through the built-in `LoggerFactoryLogger`. Other options:
  - `setup.LogLevel` (the Akka-side filter, default Info)
  - `setup.LogConfigOnStart`
  - `setup.AddLoggerFactory(myFactory)`
  - `setup.AddLogger<T>()`
  - `setup.WithDefaultLogMessageFormatter<T>()`
  - `WithLogFilter(...)` (e.g. `ExcludeMessageContaining`)
- **Semantic logging:** since Akka **1.5.57** the default formatter is `SemanticLogMessageFormatter`, and Akka.Hosting 1.5.57/1.5.59 pass named placeholders through as structured properties. VERIFIED: `_log.Info("Order {OrderId} applied {EventType}", …)` produced OTel log attributes `OrderId`, `EventType`, `ActorPath`, `LogSource`, `Thread`, `Timestamp` and `{OriginalFormat}`.
- **Context:** `log.WithContext("Tenant", "foo")` gives actor-lifetime properties. `using (log.BeginScope("RequestId", "REQ-123"))` gives per-handler properties (Akka 1.5.60+).
- **Trace correlation:** call `options.AddAkkaTraceCorrelation()` on `OpenTelemetryLoggerOptions` (Akka.Hosting ≥ 1.5.59/1.5.60). It must be **registered before exporters**. Akka logs are written asynchronously by a logger actor, so `Activity.Current` is wrong at write time. Akka.Hosting therefore captures the `ActivityContext` at log-call time and stores it as `Akka.TraceId`/`Akka.SpanId`/`Akka.TraceFlags`, and the `AkkaTraceContextProcessor` stamps them onto the `LogRecord`. VERIFIED: actor log records carried the root trace ID.
  - Correlation needs a **current Activity at log time** inside the actor, which is exactly what the envelope + `apply` span provide.
  - UNVERIFIED: whether ordering against `UseOtlpExporter()` matters. The collector received 8 log records, but I did not inspect their trace IDs.
- **Gotcha:** the MEL category for every actor log is **`Akka.Actor.ActorSystem`**, not the actor's class. Filter with the `LogSource`/`ActorPath` attributes, and set MEL log levels for category `Akka` accordingly. The Akka-side `LogLevel` and MEL's filter are independent; both must allow a level.
- The `Akka.Logger.Extensions.Logging` package (1.4.22) is obsolete. Serilog users can keep `Akka.Logger.Serilog` 1.5.69 with `SerilogLogMessageFormatter`, but MEL + OTel logs is the recommended path for this book.
- Akka 1.6 adds `LoggerSetup` for configuring loggers and formatters in code without HOCON type names (SOURCE: 1.6.0-beta1 notes).

---

## 8. Health checks (Akka.Hosting built-in)

SOURCE: Akka.Hosting README, https://petabridge.com/blog/you-dont-need-akka-healthchecks-anymore/ (2025-09-24), and the package XML docs. VERIFIED compile and run.

- The **`Akka.HealthCheck*` packages are deprecated.** They conflict with the built-in checks; uninstall them before moving to Akka.Hosting ≥ 1.5.48.1, or the app may crash at startup.
- Every check registered on `AkkaConfigurationBuilder` is added to `Microsoft.Extensions.Diagnostics.HealthChecks`. You still call `services.AddHealthChecks()` and `app.MapHealthChecks(...)` yourself; there is no `MapAkka…` helper.
- APIs:
  - `WithActorSystemLivenessCheck(HealthStatus? unHealthyStatus = null, IEnumerable<string>? tags = null)`: unhealthy once the ActorSystem has terminated. Default tag: `akka`. The sample passes `["live","akka"]` and the health output was `akka.actorsystem[live/akka]=Healthy`.
  - `WithAkkaClusterReadinessCheck(...)` (Akka.Cluster.Hosting): unhealthy until the node joins. Tags: `ready`, `akka.cluster`, `akka`. Check name `akka.cluster.join`.
  - `WithHealthCheck(name, (system, registry, ct) => Task<HealthCheckResult>, failureStatus, tags, timeout)`, or `WithHealthCheck<T>()` where `T : IAkkaHealthCheck` is resolved via `ActivatorUtilities` (1.5.51+).
  - Persistence (1.5.51+): `journalBuilder: j => j.WithHealthCheck(HealthStatus.Degraded)` and `snapshotBuilder: s => s.WithHealthCheck(...)`. These carry tags `akka`, `persistence`, and `journal` or `snapshot-store`. Akka.Persistence.Sql additionally has `WithConnectivityCheck()`, which pings the database while idle.
  - Gotcha: `WithJournal(string pluginId, Action<…>)` is **obsolete** ("will be removed in v1.6"). Use the options-based overloads or `WithSqlPersistence(..., journalBuilder:, snapshotBuilder:)`.

VERIFIED compile:

```csharp
akka.WithClustering(new ClusterOptions { Roles = ["ordering"] })
    .WithActorSystemLivenessCheck(tags: ["live"])
    .WithAkkaClusterReadinessCheck()
    .WithSqlPersistence(
        connectionString: connStr,
        providerName: ProviderName.PostgreSQL15,
        journalBuilder: journal =>
        {
            journal.WithHealthCheck(HealthStatus.Degraded);
            journal.WithConnectivityCheck();
        },
        snapshotBuilder: snapshot => snapshot.WithHealthCheck(HealthStatus.Degraded))
    .WithHealthCheck("order-registry", async (system, registry, ct) =>
        await registry.GetAsync<OrderActor>(ct) is not null
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("OrderActor not registered"),
        HealthStatus.Unhealthy, ["ready"]);

app.MapHealthChecks("/healthz/live",  new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });
```

- Align tags with the Aspire ServiceDefaults convention: `/alive` filters on the `live` tag, and `/health` runs all checks. ServiceDefaults maps these **only in Development**, so production needs explicit mapping and auth or network isolation.
- Kubernetes:
  - liveness → `live` only. Never put DB or cluster checks in liveness, or you will get restart storms.
  - readiness → `ready`, which includes cluster join and the journal.
  - startup probe → readiness with a generous threshold while recovery and cluster formation happen.
- Exclude health endpoints from tracing with the AspNetCore instrumentation `Filter`, as in the sample.

---

## 9. Backends: local and production

### 9.1 .NET Aspire dashboard (local OTLP sink)
- **Aspire 13.6.4** (NuGet). `aspire --version` reports 13.6.3 locally. Docs have moved to **https://aspire.dev** (learn.microsoft.com redirects there).
- With an AppHost, `builder.AddProject<Projects.Ordering>("ordering")` injects `OTEL_EXPORTER_OTLP_ENDPOINT` and related variables. `AddServiceDefaults()` from the ServiceDefaults project then calls `ConfigureOpenTelemetry()` (logs with `IncludeFormattedMessage`/`IncludeScopes`; AspNetCore + Http + Runtime metrics; tracing `AddSource(ApplicationName)`), `UseOtlpExporter()` when the endpoint is set, health checks (`self` tagged `live`), service discovery, and resilience. VERIFIED by generating the template.
- **Gotcha:** the template's `AddSource(builder.Environment.ApplicationName)` will **not** pick up `Bookstore.Ordering`. Add your sources and meters explicitly, plus `o.AddAkkaTraceCorrelation()` on logging.
- Standalone dashboard, no AppHost (SOURCE: https://aspire.dev/dashboard/standalone/):

  ```bash
  podman run --rm -it -p 18888:18888 -p 4317:18889 -p 4318:18890 --name aspire-dashboard \
    -e ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true \
    mcr.microsoft.com/aspire/dashboard:latest     # or mcr.microsoft.com/dotnet/aspire-dashboard:13.6.2
  export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 OTEL_EXPORTER_OTLP_PROTOCOL=grpc
  ```

  Without anonymous access, the login token is printed in the container logs. Alternatively run `aspire dashboard run --allow-anonymous`.
- The dashboard keeps telemetry **in memory with limits**, so it is for development only and not a production backend.
- The Akka.Hosting repo contains an Aspire OpenTelemetry demo: `src/Examples/Akka.Hosting.OpenTelemetry.AppHost` / `.Demo`. It is now in the akka.net repo for 1.6 (UNVERIFIED path after the move).
- `Aspire.Hosting.Testing` 13.6.4 lets integration tests spin up the AppHost. This is useful for an end-to-end "telemetry arrives" test in the testing chapter.

### 9.2 Grafana LGTM / Prometheus / Tempo / Loki
- **`grafana/otel-lgtm`** (latest tag seen: 0.39.0) is a single container with an OTel Collector, Prometheus, Tempo, Loki, Pyroscope and Grafana, exposing OTLP on 4317/4318 and Grafana on 3000. It is the best "production-shaped" local stack for the Operations chapter. Prometheus flags can be added via `PROMETHEUS_EXTRA_ARGS` (SOURCE: https://github.com/grafana/docker-otel-lgtm).
- Production shape: app → OTLP → **OpenTelemetry Collector** (`otel/opentelemetry-collector-contrib:0.142.0`, which does batching, tail sampling, attribute scrubbing and fan-out) → Tempo/Jaeger (traces), Prometheus/Mimir (metrics), Loki (logs).
- **Prometheus ≥ 3.0 accepts OTLP natively** with `--web.enable-otlp-receiver` at `/api/v1/otlp/v1/metrics` (SOURCE: https://prometheus.io/docs/guides/opentelemetry/). This is preferable to `OpenTelemetry.Exporter.Prometheus.AspNetCore`, which is **still pre-release (1.20.1-beta.1)**; do not build the book on a beta scrape endpoint.
- Name translation: OTel `ordering.command.duration` (unit `s`) becomes Prometheus `ordering_command_duration_seconds_bucket` and similar. Dots become underscores and unit suffixes are added. Mention this when showing PromQL, e.g. `histogram_quantile(0.99, sum by (le, command_type) (rate(ordering_command_duration_seconds_bucket[5m])))`. (UNVERIFIED exact suffixing under Prometheus 3's UTF-8 name support; it depends on the `translation_strategy` setting.)
- Other OTLP-native backends readers may meet: Seq, Jaeger v2, Honeycomb, Datadog, and Azure Monitor (`Azure.Monitor.OpenTelemetry.AspNetCore` 1.6.0 `UseAzureMonitor()`).
- A local collector sanity check that worked:

  ```yaml
  receivers: { otlp: { protocols: { grpc: { endpoint: 0.0.0.0:4317 } } } }
  exporters: { debug: { verbosity: basic } }
  service:
    pipelines:
      traces:  { receivers: [otlp], exporters: [debug] }
      metrics: { receivers: [otlp], exporters: [debug] }
      logs:    { receivers: [otlp], exporters: [debug] }
  ```

---

## 10. Petabridge.Cmd (live inspection)

SOURCE: https://cmd.petabridge.com/articles/RELEASE_NOTES.html. VERIFIED: compiles, and the host binds at runtime.

- Packages `Petabridge.Cmd.Host` plus palettes `Petabridge.Cmd.Cluster`, `.Cluster.Sharding`, `.Remote` and `.Remote.FailureInjection`, all **1.5.2**. CLI: `dotnet tool install --global pbm`.
- Akka.Hosting integration:

  ```csharp
  akka.AddPetabridgeCmd(cmd => cmd.RegisterCommandPalette(ClusterCommands.Instance))
  ```

  There is also an overload taking `PetabridgeCmdOptions` (host/port).
- **Gotcha (VERIFIED):** by default it binds **`0.0.0.0:9110`** (log line: `petabridge.cmd host bound to [0.0.0.0:9110]`). In production, bind it to localhost or a management network via `PetabridgeCmdOptions`, and use `kubectl exec` / port-forward to reach it.
- Useful commands:
  - `cluster show`, `cluster leave`, `cluster down-unreachable`
  - `cluster-sharding regions`, `cluster-sharding region-stats`, `cluster-sharding locate-entity`
  - `remote stats`, `remote connections`
  - **`actor alive`** (1.5.0+, needs `akka.actor.telemetry.enabled = on`): live actor counts by type, for leak detection
  - `log peek` / `log slurp`
  - `actor hierarchy`

  UNVERIFIED: exact spellings of the shard and log commands; check `pbm help` in the book build.
- In the Operations chapter, pair it with the rolling-deploy runbook (`cluster leave` before stopping a pod), even though CoordinatedShutdown on SIGTERM normally handles that.

---

## 11. Recommendations for the book (and the sample repo)

1. **Pin versions:** OpenTelemetry 1.20.1 (all stable packages), Akka 1.5.73 (with 1.6 as a forward-looking sidebar; it is beta, .NET 10+ only), Aspire 13.6.4, Petabridge.Cmd 1.5.2, and `Microsoft.Extensions.Diagnostics.Testing` 10.11.0. Bump the Aspire ServiceDefaults template from net10.0/OTel 1.17.0.
2. **Chapter flow:**
   1. BCL APIs (`ActivitySource`/`Meter`/`ILogger`)
   2. composition-root wiring
   3. *why* traces break at the mailbox (show the `<null>` probe)
   4. envelope + `AroundReceive`
   5. persistence callbacks and the stash double-count
   6. domain metrics
   7. Akka lifecycle and mailbox metrics
   8. logs with trace correlation
   9. health checks
   10. Aspire dashboard locally, then LGTM/Collector
   11. Phobos as the commercial shortcut
3. **Keep the domain pure:** aggregates call `Activity.Current?.SetTag`/`SetStatus` and the metrics class only. All OTel SDK references live in the host project. A `TracedPersistentActor` base, or a decorator, holds the plumbing.
4. **Test telemetry:** use `MetricCollector<T>` for metrics, and an `ActivityListener` or the InMemory exporter for spans. Assert parent/child relationships (trace ID equality and `ParentSpanId`), as the self-test does.
5. **Semconv:** use `messaging.system.name` (v1.45 rename), CONSUMER `process {ActorType}` spans, parents for request/response, and links for pub/sub and projections. Note that messaging semconv is still "Development" status.
6. **Cardinality discipline:** actor paths and entity IDs go on spans and logs, never on metric tags.

## 12. Source URLs
- Akka.NET releases: https://github.com/akkadotnet/akka.net/releases
- Akka.Hosting README / release notes: https://github.com/akkadotnet/Akka.Hosting (README.md, RELEASE_NOTES.md)
- Akka logging docs: https://getakka.net/articles/utilities/logging.html
- Built-in health checks: https://petabridge.com/blog/you-dont-need-akka-healthchecks-anymore/
- Phobos release notes: https://phobos.petabridge.com/articles/releases/RELEASE_NOTES.html ; product: https://petabridge.com/platform/phobos/
- Petabridge.Cmd release notes: https://cmd.petabridge.com/articles/RELEASE_NOTES.html
- OTel .NET releases: https://github.com/open-telemetry/opentelemetry-dotnet/releases ; docs: https://opentelemetry.io/docs/languages/dotnet/
- Messaging semconv: https://opentelemetry.io/docs/specs/semconv/messaging/messaging-spans/ ; changelog: https://github.com/open-telemetry/semantic-conventions/blob/main/CHANGELOG.md
- .NET 11 library changes: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries
- Aspire standalone dashboard: https://aspire.dev/dashboard/standalone/
- Prometheus OTLP: https://prometheus.io/docs/guides/opentelemetry/
- Grafana LGTM image: https://github.com/grafana/docker-otel-lgtm
- NuGet versions: https://api.nuget.org/v3-flatcontainer/<id>/index.json
