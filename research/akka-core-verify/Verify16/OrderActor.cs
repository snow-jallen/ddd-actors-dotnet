using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Event;
using Akka.Persistence;
using Akka.Persistence.Journal;
using Shop.Domain;

namespace Shop.Actors;

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
        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is OrderState s) _state = s;
        });
        Recover<IOrderEvent>(e => _state = _state.Apply(OrderState.ToUnion(e)));
        Recover<RecoveryCompleted>(_ => _log.Info("{0} recovered at seqNr {1}", PersistenceId, LastSequenceNr));

        // ---- commands ----
        Command<GetOrder>(_ => Sender.Tell(View()));
        Command<IOrderCommand>(HandleCommand);

        // ---- snapshot housekeeping ----
        Command<SaveSnapshotSuccess>(ok =>
        {
            // keep only the latest snapshot; optionally DeleteMessages(ok.Metadata.SequenceNr)
            DeleteSnapshots(new SnapshotSelectionCriteria(ok.Metadata.SequenceNr - 1));
        });
        Command<SaveSnapshotFailure>(f => _log.Warning(f.Cause, "snapshot failed"));
        Command<DeleteSnapshotsSuccess>(_ => { });
        Command<DeleteSnapshotsFailure>(f => _log.Warning(f.Cause, "delete snapshots failed"));
    }

    private void HandleCommand(IOrderCommand cmd)
    {
        switch (_state.Decide(cmd))
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

/// <summary>Write-side event adapter: tags every order event so Persistence Query can find it.</summary>
public sealed class OrderTaggingAdapter : IWriteEventAdapter
{
    public string Manifest(object evt) => string.Empty;
    public object ToJournal(object evt) => evt switch
    {
        IOrderEvent e => new Tagged(e, ["order", $"order-{e.GetType().Name}"]),
        _ => evt
    };
}

public sealed class OrderMessageExtractor(int maxShards) : HashCodeMessageExtractor(maxShards)
{
    public override string EntityId(object message) => message switch
    {
        IOrderCommand c => c.OrderId,
        _ => null!
    };
}

/// <summary>Marker type used as the ActorRegistry key for the shard region.</summary>
public sealed class OrderRegion;
