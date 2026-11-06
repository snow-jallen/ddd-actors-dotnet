using System.Diagnostics;
using System.Diagnostics.Metrics;
using Akka.Actor;

namespace OtelAkka;

/// <summary>
/// Subscribes to Akka's built-in actor telemetry events (akka.actor.telemetry.enabled = on, Akka >= 1.4.47)
/// and turns them into OTel metrics: live actors by type, restarts by type.
/// </summary>
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
