using System.Diagnostics.Metrics;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace OtelAkka;

/// <summary>Mailbox depth as an ObservableGauge. ActorCell.NumberOfMessages reads the mailbox queue count,
/// which is safe to read from the collector thread (approximate by nature).</summary>
public sealed class MailboxDepthProbe : ReceiveActor
{
    private static readonly Meter Meter = new("Akka.Mailbox", "1.0.0");
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<IActorRef, (string Type, ActorCell Cell)> Watched = new();
    private static readonly ObservableGauge<int> Gauge = Meter.CreateObservableGauge("akka.mailbox.depth",
        () => Watched.Values.GroupBy(w => w.Type)
                     .Select(g => new Measurement<int>(g.Sum(w => w.Cell.NumberOfMessages),
                                                       new KeyValuePair<string, object?>("actor.type", g.Key))),
        unit: "{message}", description: "Messages waiting in mailboxes, summed by actor type");

    public static void Track(IUntypedActorContext ctx) => Watched[ctx.Self] = (ctx.Props.Type.Name, (ActorCell)ctx);
    public static void Untrack(IActorRef self) => Watched.TryRemove(self, out _);
}

public static class MetricsSelfCheck
{
    /// <summary>Unit-test style check using MetricCollector (Microsoft.Extensions.Diagnostics.Testing).</summary>
    public static long Run()
    {
        var services = new ServiceCollection().AddMetrics().AddSingleton<OrderingMetrics>().BuildServiceProvider();
        var metrics = services.GetRequiredService<OrderingMetrics>();
        var meterFactory = services.GetRequiredService<IMeterFactory>();
        using var collector = new MetricCollector<long>(meterFactory, OrderingDiagnostics.SourceName, "ordering.commands.processed");
        metrics.CommandsProcessed.Add(1, new KeyValuePair<string, object?>("command.type", "PlaceOrder"));
        var last = collector.GetMeasurementSnapshot().Single();
        return last.Value + (last.Tags["command.type"] as string == "PlaceOrder" ? 0 : 1000);
    }
}

public static class Net11TracingProbe
{
    /// <summary>.NET 11: AddTracing() + ActivitySourceFactory (DI-created sources, rule-based enable/disable).</summary>
    public static string Run()
    {
        var sp = new ServiceCollection().AddTracing().BuildServiceProvider();
        var factory = sp.GetService<System.Diagnostics.ActivitySourceFactory>();
        var src = factory?.Create(new System.Diagnostics.ActivitySourceOptions(OrderingDiagnostics.SourceName) { Version = "1.0.0" });
        return $"factory={(factory?.GetType().Name ?? "null")} source={src?.Name}/{src?.Version} type={src?.GetType().Name}";
    }
}
