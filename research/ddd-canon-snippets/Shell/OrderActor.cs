using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Persistence;
using Sales.Domain;

namespace Sales.Shell;

// Envelope + reply protocol (actor-facing; not part of the domain)
public sealed record OrderEnvelope(string OrderId, OrderCommand Command);
public sealed record CommandAccepted(int EventCount);
public sealed record CommandRejected(OrderError Error);
public sealed record GetOrderState(string OrderId);

/// Actor shell: owns persistence, identity, concurrency (one message at a time).
/// All business decisions are delegated to the pure functional core (Order.Decide / Order.Evolve).
public sealed class OrderActor : ReceivePersistentActor
{
    private readonly TimeProvider _clock;
    private OrderState _state = Order.Initial;
    private int _eventsSinceSnapshot;

    public override string PersistenceId { get; }

    public OrderActor(string orderId, TimeProvider clock)
    {
        PersistenceId = $"order-{orderId}";
        _clock = clock;

        // Persist the *case types*, never the union wrapper struct, so the journal
        // stores plain records the serializer can resolve.
        Recover<OrderPlaced>(e => _state = Order.Evolve(_state, e));
        Recover<LineAdded>(e => _state = Order.Evolve(_state, e));
        Recover<OrderSubmitted>(e => _state = Order.Evolve(_state, e));
        Recover<OrderCancelled>(e => _state = Order.Evolve(_state, e));
        Recover<SnapshotOffer>(s => { if (s.Snapshot is OrderSnapshot snap) _state = snap.ToState(); });

        Command<OrderEnvelope>(env => Handle(env.Command));
        Command<GetOrderState>(_ => Sender.Tell(_state));
        Command<SaveSnapshotSuccess>(_ => { });
    }

    private void Handle(OrderCommand cmd)
    {
        switch (Order.Decide(_state, cmd, _clock))
        {
            case OrderError err:
                Sender.Tell(new CommandRejected(err));
                break;
            case OrderEvent[] events when events.Length == 0:
                Sender.Tell(new CommandAccepted(0)); // idempotent no-op
                break;
            case OrderEvent[] events:
                var replyTo = Sender;
                // PersistAll: atomic write of all events from one decision
                PersistAll(events.Select(e => e.Value!), raw =>
                {
                    _state = Order.Evolve(_state, ToEvent(raw));
                    if (++_eventsSinceSnapshot >= 100) { SaveSnapshot(OrderSnapshot.From(_state)); _eventsSinceSnapshot = 0; }
                });
                DeferAsync(events.Length, n => replyTo.Tell(new CommandAccepted(n)));
                break;
        }
    }

    private static OrderEvent ToEvent(object raw) => raw switch
    {
        OrderPlaced e => e, LineAdded e => e, OrderSubmitted e => e, OrderCancelled e => e,
        _ => throw new InvalidOperationException($"Unknown event {raw.GetType()}"),
    };

    public static Props PropsFor(string orderId, TimeProvider clock) =>
        Props.Create(() => new OrderActor(orderId, clock));
}

// Snapshot DTO: keep persisted shapes separate from in-memory unions.
public sealed record OrderSnapshot(string Kind, OrderId Id, string? CustomerId, OrderLine[] Lines, Money? Total)
{
    public static OrderSnapshot From(OrderState s) => s switch
    {
        NoOrder => new("none", default, null, [], null),
        DraftOrder d => new("draft", d.Id, d.CustomerId, [.. d.Lines], null),
        SubmittedOrder o => new("submitted", o.Id, null, [], o.Total),
        CancelledOrder c => new("cancelled", c.Id, null, [], null),
    };
    public OrderState ToState() => Kind switch
    {
        "draft" => new DraftOrder(Id, CustomerId!, [.. Lines]),
        "submitted" => new SubmittedOrder(Id, Total!),
        "cancelled" => new CancelledOrder(Id),
        _ => new NoOrder(),
    };
}

// Sharding "repository": routes by aggregate id, location-transparent.
public sealed class OrderMessageExtractor() : HashCodeMessageExtractor(maxNumberOfShards: 100)
{
    public override string? EntityId(object message) => message switch
    {
        OrderEnvelope e => e.OrderId,
        GetOrderState q => q.OrderId,
        _ => null,
    };
}
