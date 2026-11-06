using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Akka.Actor;

namespace OtelAkka;

/// <summary>One ActivitySource + one Meter per bounded context. Names are the contract with the OTel SDK.</summary>
public static class OrderingDiagnostics
{
    public const string SourceName = "Bookstore.Ordering";   // used for both ActivitySource and Meter
    public static readonly ActivitySource Source = new(SourceName, "1.0.0");
}

/// <summary>
/// Metrics instruments created from IMeterFactory (DI-friendly, testable with MetricCollector&lt;T&gt;).
/// Register as a singleton; actors receive it via constructor injection (Akka.DependencyInjection).
/// </summary>
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

/// <summary>
/// Envelope carrying W3C trace context across an actor mailbox. Strings, not ActivityContext,
/// so it serializes across Akka.Remote / Cluster.Sharding unchanged.
/// </summary>
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
    /// <summary>Tell that carries the caller's Activity.Current as W3C traceparent.</summary>
    public static void TellTraced(this IActorRef target, object message, IActorRef? sender = null) =>
        target.Tell(TraceEnvelope.Wrap(message), sender ?? ActorRefs.NoSender);

    public static Task<T> AskTraced<T>(this IActorRef target, object message, TimeSpan timeout) =>
        target.Ask<T>(TraceEnvelope.Wrap(message), timeout);

    /// <summary>Unwraps C# 15 unions so spans are named after the case, not the union struct.</summary>
    public static string MessageName(object message) => message switch
    {
        IUnion u when u.Value is not null => u.Value.GetType().Name,
        _ => message.GetType().Name
    };
}
