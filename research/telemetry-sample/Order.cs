using System.Diagnostics;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;

namespace OtelAkka;

// ----- Domain messages (C# 15 unions on .NET 11) -----
public sealed record PlaceOrder(string OrderId, string Sku, int Quantity);
public sealed record CancelOrder(string OrderId, string Reason);
public union OrderCommand(PlaceOrder, CancelOrder);

public sealed record OrderPlaced(string OrderId, string Sku, int Quantity);
public sealed record OrderCancelled(string OrderId, string Reason);
public union OrderEvent(OrderPlaced, OrderCancelled);

public sealed record CommandResult(bool Accepted, string? Error = null);
public sealed record WhatIsCurrentActivity;  // diagnostic probe

/// <summary>
/// Persistent-actor base that turns a TraceEnvelope into a CONSUMER "process" span around the handler,
/// and records command duration. Everything else passes through untouched.
/// </summary>
public abstract class TracedPersistentActor : ReceivePersistentActor
{
    protected readonly OrderingMetrics Metrics;
    private readonly string _actorType;
    private bool _handlerInvoked;   // false after base.AroundReceive => Eventsourced stashed the message

    protected TracedPersistentActor(OrderingMetrics metrics)
    {
        Metrics = metrics;
        _actorType = GetType().Name;
    }

    protected override bool AroundReceive(Receive receive, object message)
    {
        if (message is not TraceEnvelope env)
            return base.AroundReceive(receive, message);

        var messageName = TracedTellExtensions.MessageName(env.Message);
        // Messaging semconv: span kind CONSUMER, name "{operation} {destination}".
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

    /// <summary>Use instead of Command&lt;T&gt; so the base class knows the handler really ran.</summary>
    protected void TracedCommand<T>(Action<T> handler) => Command<T>(m => { _handlerInvoked = true; handler(m); });

    /// <summary>
    /// Persist + record a "persist" span and latency histogram. The span is created retroactively in the
    /// callback with an explicit start time, so Activity.Current is never left dangling between messages.
    /// </summary>
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
            // The persist callback runs *outside* the original process span; re-establish it as parent
            // for anything the handler emits (replies, logs) by starting a short child span.
            using var cb = OrderingDiagnostics.Source.StartActivity($"apply {_actorType}", ActivityKind.Internal, parent);
            handler(e);
        });
    }
}

public sealed class OrderActor : TracedPersistentActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private OrderState _state = OrderState.Empty;
    public override string PersistenceId { get; }

    public OrderActor(string orderId, OrderingMetrics metrics) : base(metrics)
    {
        PersistenceId = $"order-{orderId}";

        Recover<OrderEvent>(e => _state = _state.Apply(e));

        TracedCommand<OrderCommand>(cmd =>
        {
            // Exhaustive switch over the union: a missing case is warning CS8509 (make it an error in the csproj).
            OrderEvent? decision = cmd switch
            {
                PlaceOrder p when _state.Placed => Reject("already placed"),
                PlaceOrder p => new OrderPlaced(p.OrderId, p.Sku, p.Quantity),
                CancelOrder c when !_state.Placed => Reject("not placed"),
                CancelOrder c => new OrderCancelled(c.OrderId, c.Reason),
            };
            if (decision is not { } evt) return;

            var replyTo = Sender;
            PersistTraced(evt, e =>
            {
                _state = _state.Apply(e);
                // Semantic (structured) logging: named placeholders become properties (Akka >= 1.5.57).
                _log.Info("Order {OrderId} applied {EventType}", PersistenceId, TracedTellExtensions.MessageName(e));
                replyTo.Tell(new CommandResult(true));
            });
        });

        TracedCommand<WhatIsCurrentActivity>(_ =>
            Sender.Tell(Activity.Current?.TraceId.ToString() ?? "<null>"));
    }

    private OrderEvent? Reject(string why)
    {
        Metrics.CommandsRejected.Add(1, new TagList { { "aggregate.type", nameof(OrderActor) }, { "reason", why } });
        Activity.Current?.SetStatus(ActivityStatusCode.Error, why);
        Sender.Tell(new CommandResult(false, why));
        return null;
    }

    protected override void PreStart() { Metrics.ActiveAggregates.Add(1); MailboxDepthProbe.Track(Context); base.PreStart(); }
    protected override void PostStop() { Metrics.ActiveAggregates.Add(-1); MailboxDepthProbe.Untrack(Self); base.PostStop(); }
}

public sealed record OrderState(bool Placed, bool Cancelled)
{
    public static readonly OrderState Empty = new(false, false);
    public OrderState Apply(OrderEvent e) => e switch
    {
        OrderPlaced => this with { Placed = true },
        OrderCancelled => this with { Cancelled = true },
    };
}
