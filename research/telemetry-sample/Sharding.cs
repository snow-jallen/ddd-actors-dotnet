using System.Diagnostics.Metrics;
using Akka.Actor;
using Akka.Cluster.Sharding;

namespace OtelAkka;

/// <summary>Sharding must see THROUGH the trace envelope to route, but deliver the envelope intact.</summary>
public sealed class TracedOrderExtractor(int maxShards) : HashCodeMessageExtractor(maxShards)
{
    public override string? EntityId(object message) => message switch
    {
        TraceEnvelope { Message: var inner } => EntityId(inner),
        OrderCommand c => c switch { PlaceOrder p => p.OrderId, CancelOrder x => x.OrderId },
        ShardRegion.StartEntity s => s.EntityId,
        _ => null
    };

    // Deliver the whole envelope to the entity so it can continue the trace.
    public override object EntityMessage(object message) => message;
}

/// <summary>Polls the local shard region and publishes shard/entity counts as gauges.</summary>
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
