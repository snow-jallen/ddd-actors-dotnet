using Akka.Actor;
using Akka.Persistence;
using Sales.Domain;

namespace Sales.Shell;

// Messages to/from other bounded contexts (integration contracts, versioned)
public sealed record ReservePayment(OrderId Order, Money Amount, string IdempotencyKey);
public sealed record PaymentReserved(OrderId Order);
public sealed record PaymentDeclined(OrderId Order, string Reason);
public sealed record PaymentTimedOut(OrderId Order);

// Process-manager state machine as a union, transitions as a pure function
public sealed record AwaitingPayment(OrderId Order, int Attempts);
public sealed record Fulfilled(OrderId Order);
public sealed record Compensated(OrderId Order, string Reason);
public union FulfillmentState(AwaitingPayment, Fulfilled, Compensated);

public static class FulfillmentPolicy
{
    public const int MaxAttempts = 3;
    public static FulfillmentState Next(FulfillmentState s, object input) => (s, input) switch
    {
        (AwaitingPayment a, PaymentReserved) => new Fulfilled(a.Order),
        (AwaitingPayment a, PaymentDeclined d) => new Compensated(a.Order, d.Reason),
        (AwaitingPayment a, PaymentTimedOut) when a.Attempts + 1 >= MaxAttempts => new Compensated(a.Order, "timeout"),
        (AwaitingPayment a, PaymentTimedOut) => a with { Attempts = a.Attempts + 1 },
        _ => s,
    };
}

public sealed class FulfillmentProcess : ReceivePersistentActor, IWithTimers
{
    private FulfillmentState _state;
    private readonly IActorRef _payments;
    private readonly IActorRef _orders;
    private readonly Money _amount;
    public ITimerScheduler Timers { get; set; } = null!;
    public override string PersistenceId { get; }

    public FulfillmentProcess(OrderSubmitted started, IActorRef payments, IActorRef orders)
    {
        PersistenceId = $"fulfillment-{started.Id}";
        _state = new AwaitingPayment(started.Id, 0);
        _payments = payments; _orders = orders; _amount = started.Total;

        Recover<PaymentReserved>(e => _state = FulfillmentPolicy.Next(_state, e));
        Recover<PaymentDeclined>(e => _state = FulfillmentPolicy.Next(_state, e));
        Recover<PaymentTimedOut>(e => _state = FulfillmentPolicy.Next(_state, e));
        Recover<RecoveryCompleted>(_ => Resume());

        Command<PaymentReserved>(e => Persist(e, Apply));
        Command<PaymentDeclined>(e => Persist(e, Apply));
        Command<PaymentTimedOut>(e => Persist(e, Apply));
    }

    private void Apply(object e) { _state = FulfillmentPolicy.Next(_state, e); Resume(); }

    // Side effects live in the shell, driven by state; idempotency key makes retries safe.
    private void Resume()
    {
        switch (_state)
        {
            case AwaitingPayment a:
                _payments.Tell(new ReservePayment(a.Order, _amount, $"{a.Order}-pay"));
                Timers.StartSingleTimer("pay-timeout", new PaymentTimedOut(a.Order), TimeSpan.FromSeconds(30));
                break;
            case Fulfilled:
                Timers.CancelAll(); Context.Stop(Self); break;
            case Compensated c:
                Timers.CancelAll();
                _orders.Tell(new OrderEnvelope(c.Order.ToString(), new CancelOrder(c.Order, c.Reason)));
                Context.Stop(Self); break;
        }
    }
}
