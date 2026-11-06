using System.Diagnostics;
using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OtelAkka;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;

var selfTest = args.Contains("--selftest");
var builder = WebApplication.CreateBuilder(args);

// In-memory sinks for --selftest verification
var spans = new List<Activity>();
var metrics = new List<Metric>();
var logs = new List<LogRecord>();

// ---------- OpenTelemetry: logs, traces, metrics ----------
builder.Logging.AddOpenTelemetry(o =>
{
    o.IncludeFormattedMessage = true;
    o.IncludeScopes = true;
    o.AddAkkaTraceCorrelation();          // Akka.Hosting >= 1.5.59: must come BEFORE exporters
    if (selfTest) o.AddInMemoryExporter(logs);
});

builder.Services.AddSingleton<OrderingMetrics>();

var otel = builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName: "ordering-api", serviceVersion: "1.2.3")
        .AddAttributes([new("deployment.environment.name", builder.Environment.EnvironmentName)]))
    .WithTracing(t =>
    {
        t.AddSource(OrderingDiagnostics.SourceName)
         .AddSource("Test.Http")
         .AddAspNetCoreInstrumentation(o => o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/healthz"))
         .AddHttpClientInstrumentation();
        if (selfTest) t.AddInMemoryExporter(spans);
    })
    .WithMetrics(m =>
    {
        m.AddMeter(OrderingDiagnostics.SourceName, "Akka.Lifecycle", "Akka.Mailbox")
         .AddAspNetCoreInstrumentation()
         .AddHttpClientInstrumentation()
         .AddRuntimeInstrumentation();
        if (selfTest) m.AddInMemoryExporter(metrics);
    });

// OTLP only when an endpoint is configured (Aspire sets OTEL_EXPORTER_OTLP_ENDPOINT for you)
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
    otel.UseOtlpExporter();

// ---------- Akka.NET via Akka.Hosting ----------
builder.Services.AddHealthChecks();
builder.Services.AddAkka("ordering", (akka, sp) =>
{
    akka.AddHocon("akka.actor.telemetry.enabled = on", HoconAddMode.Prepend)   // emits ActorStarted/Stopped/Restarted
        .ConfigureLoggers(setup =>
        {
            setup.LogLevel = Akka.Event.LogLevel.InfoLevel;
            setup.ClearLoggers();
            setup.AddLoggerFactory();     // route Akka logs into Microsoft.Extensions.Logging (and thus OTel)
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

var app = builder.Build();
app.MapHealthChecks("/healthz/live", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });
app.MapPost("/orders/{id}", async (string id, IRequiredActor<OrderActor> order) =>
{
    var result = await order.ActorRef.AskTraced<CommandResult>(new OrderCommand(new PlaceOrder(id, "SKU-1", 1)), TimeSpan.FromSeconds(3));
    return result.Accepted ? Results.Accepted() : Results.Conflict(result.Error);
});

if (!selfTest) { app.Run(); return; }

// ---------- --selftest: prove propagation works ----------
await app.StartAsync();
var actor = await app.Services.GetRequiredService<ActorRegistry>().GetAsync<OrderActor>();
using var testSource = new ActivitySource("Test.Http");
string rootTraceId;
using (var root = testSource.StartActivity("POST /orders/o-1", ActivityKind.Server))
{
    rootTraceId = root!.TraceId.ToString();
    var plain = await actor.Ask<string>(new WhatIsCurrentActivity(), TimeSpan.FromSeconds(3));
    var wrapped = await actor.AskTraced<string>(new WhatIsCurrentActivity(), TimeSpan.FromSeconds(3));
    Console.WriteLine($"root trace           = {rootTraceId}");
    Console.WriteLine($"Activity.Current in actor, plain Tell    = {plain}");
    Console.WriteLine($"Activity.Current in actor, TraceEnvelope = {wrapped}");

    // fire two commands back-to-back so the 2nd is stashed by Eventsourced during the 1st persist
    var t1 = actor.AskTraced<CommandResult>(new OrderCommand(new PlaceOrder("o-1", "SKU-1", 2)), TimeSpan.FromSeconds(3));
    var t2 = actor.AskTraced<CommandResult>(new OrderCommand(new CancelOrder("o-1", "changed mind")), TimeSpan.FromSeconds(3));
    var t3 = actor.AskTraced<CommandResult>(new OrderCommand(new PlaceOrder("o-1", "SKU-1", 2)), TimeSpan.FromSeconds(3));
    Console.WriteLine($"results: {await t1} | {await t2} | {await t3}");
}
await Task.Delay(500);
app.Services.GetRequiredService<TracerProvider>().ForceFlush();
app.Services.GetRequiredService<MeterProvider>().ForceFlush();
app.Services.GetRequiredService<LoggerProvider>().ForceFlush();

Console.WriteLine("\n--- spans ---");
foreach (var s in spans.OrderBy(s => s.StartTimeUtc))
    Console.WriteLine($"{s.DisplayName,-28} kind={s.Kind,-8} trace={(s.TraceId.ToString() == rootTraceId ? "ROOT" : s.TraceId.ToString()[..8])} " +
                      $"span={s.SpanId.ToString()[..6]} parent={(s.ParentSpanId == default ? "-" : s.ParentSpanId.ToString()[..6])} " +
                      $"msg={s.GetTagItem("akka.message.type") ?? s.GetTagItem("event.type")}{(s.GetTagItem("akka.message.stashed") is true ? " (STASHED)" : "")} status={s.Status} {s.Duration.TotalMilliseconds:F2}ms");

Console.WriteLine("\n--- actor log records (trace correlation) ---");
foreach (var l in logs.Where(l => l.CategoryName?.Contains("OrderActor") == true || (l.FormattedMessage ?? "").Contains("Order order-")))
    Console.WriteLine($"[{l.CategoryName}] {l.FormattedMessage} trace={(l.TraceId.ToString() == rootTraceId ? "ROOT" : l.TraceId.ToString())} attrs={string.Join(",", l.Attributes?.Select(a => a.Key) ?? [])}");

Console.WriteLine("\n--- custom metrics ---");
foreach (var m in metrics.Where(m => m.MeterName == OrderingDiagnostics.SourceName || m.MeterName == "Akka.Mailbox").DistinctBy(m => m.Name))
{
    foreach (ref readonly var p in m.GetMetricPoints())
    {
        var tags = new List<string>();
        foreach (var t in p.Tags) tags.Add($"{t.Key}={t.Value}");
        var value = m.MetricType switch
        {
            MetricType.Histogram => $"count={p.GetHistogramCount()} sum={p.GetHistogramSum():F5}",
            MetricType.LongSum or MetricType.LongSumNonMonotonic => p.GetSumLong().ToString(),
            MetricType.LongGauge => p.GetGaugeLastValueLong().ToString(),
            _ => "?"
        };
        Console.WriteLine($"{m.Name} [{string.Join(",", tags)}] {value}");
    }
}

var health = await app.Services.GetRequiredService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>().CheckHealthAsync();
Console.WriteLine($"\nhealth: {health.Status} -> {string.Join(", ", health.Entries.Select(e => $"{e.Key}[{string.Join("/", e.Value.Tags)}]={e.Value.Status}"))}");
Console.WriteLine(Net11TracingProbe.Run());
Console.WriteLine($"MetricCollector check value = {MetricsSelfCheck.Run()} (expect 1)");
await app.StopAsync();
