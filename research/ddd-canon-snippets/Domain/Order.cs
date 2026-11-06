namespace Sales.Domain;

// ---------- Value objects ----------
public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString("N");
}

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }
    private Money(decimal amount, string currency) { Amount = amount; Currency = currency; }

    public static Result<Money> Create(decimal amount, string currency) =>
        currency is { Length: 3 } && amount >= 0
            ? new Money(amount, currency.ToUpperInvariant())
            : new DomainError.InvalidValue(nameof(Money), $"{amount} {currency}");

    // Closure of operations: Money + Money -> Money
    public Money Add(Money other) =>
        other.Currency == Currency
            ? new Money(Amount + other.Amount, Currency)
            : throw new InvalidOperationException("Currency mismatch");

    public Money Times(int qty) => new(Amount * qty, Currency);
    public static Money Zero(string currency) => new(0m, currency);
}

public sealed record Sku(string Value);
public sealed record OrderLine(Sku Sku, int Quantity, Money UnitPrice)
{
    public Money LineTotal => UnitPrice.Times(Quantity);
}

// ---------- Errors / results as unions ----------
public sealed record OrderNotFound(OrderId Id);
public static class DomainError
{
    public sealed record InvalidValue(string Type, string Raw);
    public sealed record InvariantViolated(string Rule);
    public sealed record WrongState(string Current, string Attempted);
}
public union OrderError(DomainError.InvalidValue, DomainError.InvariantViolated, DomainError.WrongState);

// A tiny generic Result: union of value or error
public union Result<T>(T, DomainError.InvalidValue);

// ---------- Commands (intent) ----------
public sealed record PlaceOrder(OrderId Id, string CustomerId);
public sealed record AddLine(OrderId Id, OrderLine Line);
public sealed record SubmitOrder(OrderId Id);
public sealed record CancelOrder(OrderId Id, string Reason);
public union OrderCommand(PlaceOrder, AddLine, SubmitOrder, CancelOrder);

// ---------- Domain events (facts, past tense) ----------
public sealed record OrderPlaced(OrderId Id, string CustomerId, DateTimeOffset At);
public sealed record LineAdded(OrderId Id, OrderLine Line);
public sealed record OrderSubmitted(OrderId Id, Money Total, DateTimeOffset At);
public sealed record OrderCancelled(OrderId Id, string Reason);
public union OrderEvent(OrderPlaced, LineAdded, OrderSubmitted, OrderCancelled);

// ---------- Aggregate state as a union of lifecycle states ----------
public sealed record NoOrder;
public sealed record DraftOrder(OrderId Id, string CustomerId, System.Collections.Immutable.ImmutableList<OrderLine> Lines);
public sealed record SubmittedOrder(OrderId Id, Money Total);
public sealed record CancelledOrder(OrderId Id);
public union OrderState(NoOrder, DraftOrder, SubmittedOrder, CancelledOrder);

// ---------- Functional core: Decide + Evolve ----------
public union Decision(OrderEvent[], OrderError);

public static class Order
{
    public const int MaxLines = 50;
    public static readonly OrderState Initial = new NoOrder();

    public static Decision Decide(OrderState state, OrderCommand cmd, TimeProvider clock) =>
        (state, cmd) switch
        {
            (NoOrder, PlaceOrder p) =>
                new OrderEvent[] { new OrderPlaced(p.Id, p.CustomerId, clock.GetUtcNow()) },

            (DraftOrder d, AddLine a) when d.Lines.Count >= MaxLines =>
                new OrderError(new DomainError.InvariantViolated($"An order may have at most {MaxLines} lines")),
            (DraftOrder d, AddLine a) when a.Line.Quantity <= 0 =>
                new OrderError(new DomainError.InvariantViolated("Quantity must be positive")),
            (DraftOrder, AddLine a) =>
                new OrderEvent[] { new LineAdded(a.Id, a.Line) },

            (DraftOrder d, SubmitOrder) when d.Lines.IsEmpty =>
                new OrderError(new DomainError.InvariantViolated("Cannot submit an empty order")),
            (DraftOrder d, SubmitOrder s) =>
                new OrderEvent[] { new OrderSubmitted(s.Id, Total(d), clock.GetUtcNow()) },

            (DraftOrder or SubmittedOrder, CancelOrder c) =>
                new OrderEvent[] { new OrderCancelled(c.Id, c.Reason) },

            // Idempotency: cancelling twice is a no-op, not an error
            (CancelledOrder, CancelOrder) => Array.Empty<OrderEvent>(),

            var (s, c) => new OrderError(new DomainError.WrongState(s.Value?.GetType().Name ?? "null", c.Value?.GetType().Name ?? "null")),
        };

    public static OrderState Evolve(OrderState state, OrderEvent evt) =>
        (state, evt) switch
        {
            (NoOrder, OrderPlaced p) => new DraftOrder(p.Id, p.CustomerId, []),
            (DraftOrder d, LineAdded l) => d with { Lines = d.Lines.Add(l.Line) },
            (DraftOrder, OrderSubmitted s) => new SubmittedOrder(s.Id, s.Total),
            (_, OrderCancelled c) => new CancelledOrder(c.Id),
            _ => state, // events are facts; unknown combos are ignored during replay
        };

    // Side-effect-free function
    public static Money Total(DraftOrder d) =>
        d.Lines.Aggregate(Money.Zero(d.Lines.FirstOrDefault()?.UnitPrice.Currency ?? "USD"), (acc, l) => acc.Add(l.LineTotal));

    // Exhaustive switch over event union: no default arm needed
    public static string Describe(OrderEvent e) => e switch
    {
        OrderPlaced p => $"placed by {p.CustomerId}",
        LineAdded l => $"added {l.Line.Quantity} x {l.Line.Sku.Value}",
        OrderSubmitted s => $"submitted for {s.Total.Amount} {s.Total.Currency}",
        OrderCancelled c => $"cancelled: {c.Reason}",
    };
}

// ---------- Specification ----------
public interface ISpecification<in T> { bool IsSatisfiedBy(T candidate); }
public sealed record LargeOrderSpec(decimal Threshold) : ISpecification<SubmittedOrder>
{
    public bool IsSatisfiedBy(SubmittedOrder o) => o.Total.Amount >= Threshold;
}
