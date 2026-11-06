using Akka.Actor;
using Akka.Event;
using Akka.Persistence;

namespace Cart.Api;

// ---------- Value objects ----------
public readonly record struct Sku
{
    public string Value { get; }
    public Sku(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 32)
            throw new ArgumentException("SKU must be 1-32 non-blank chars", nameof(value));
        Value = value.Trim().ToUpperInvariant();
    }
    public override string ToString() => Value;
}

public readonly record struct Quantity
{
    public int Value { get; }
    public Quantity(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 99);
        Value = value;
    }
    public static Quantity operator +(Quantity a, Quantity b) => new(Math.Min(99, a.Value + b.Value));
}

// ---------- Commands / events / outcomes as C# unions ----------
public sealed record AddItem(string CartId, Sku Sku, Quantity Qty);
public sealed record CheckOut(string CartId);
public union CartCommand(AddItem, CheckOut);

public sealed record ItemAdded(string CartId, Sku Sku, Quantity Qty);
public sealed record CheckedOut(string CartId, int LineCount);
public union CartEvent(ItemAdded, CheckedOut);

public sealed record Rejected(string Reason);

// ---------- Aggregate: pure decide/evolve ----------
public sealed record CartState(string CartId, IReadOnlyDictionary<Sku, Quantity> Lines, bool IsCheckedOut)
{
    public static CartState Empty(string id) => new(id, new Dictionary<Sku, Quantity>(), false);
}

public static class Cart
{
    public const int MaxLines = 10;

    public static IReadOnlyList<CartEvent> Decide(CartState state, CartCommand command) => command switch
    {
        AddItem _ when state.IsCheckedOut => throw new CartRuleViolation("Cart already checked out"),
        AddItem a when !state.Lines.ContainsKey(a.Sku) && state.Lines.Count >= MaxLines
            => throw new CartRuleViolation($"Cart cannot exceed {MaxLines} lines"),
        AddItem a => [new ItemAdded(a.CartId, a.Sku, a.Qty)],
        CheckOut _ when state.IsCheckedOut => [],               // idempotent
        CheckOut _ when state.Lines.Count == 0 => throw new CartRuleViolation("Cannot check out an empty cart"),
        CheckOut c => [new CheckedOut(c.CartId, state.Lines.Count)],
    };

    public static CartState Evolve(CartState state, CartEvent @event) => @event switch
    {
        ItemAdded e => state with
        {
            Lines = new Dictionary<Sku, Quantity>(state.Lines)
            {
                [e.Sku] = state.Lines.TryGetValue(e.Sku, out var q) ? q + e.Qty : e.Qty
            }
        },
        CheckedOut => state with { IsCheckedOut = true },
    };
}

public sealed class CartRuleViolation(string message) : Exception(message);

// ---------- Persistent actor hosting the aggregate ----------
public sealed record GetCart(string CartId);

public sealed class CartActor : ReceivePersistentActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private CartState _state;
    public override string PersistenceId { get; }

    public CartActor(string cartId)
    {
        PersistenceId = $"cart-{cartId}";
        _state = CartState.Empty(cartId);

        Recover<ItemAdded>(e => _state = Cart.Evolve(_state, e));
        Recover<CheckedOut>(e => _state = Cart.Evolve(_state, e));

        Command<AddItem>(c => Handle(c));
        Command<CheckOut>(c => Handle(c));
        Command<GetCart>(_ => Sender.Tell(_state));
    }

    private void Handle(CartCommand cmd)
    {
        IReadOnlyList<CartEvent> events;
        try { events = Cart.Decide(_state, cmd); }
        catch (CartRuleViolation ex)
        {
            _log.Warning("Rejected {0}: {1}", cmd.Value!.GetType().Name, ex.Message);
            Sender.Tell(new Rejected(ex.Message));
            return;
        }
        if (events.Count == 0) { Sender.Tell(_state); return; }

        // Persist the *case* objects (records), not the union struct, so the journal
        // and serializer see ordinary CLR types.
        var replyTo = Sender;
        PersistAll(events.Select(e => e.Value!), e =>
        {
            _state = Cart.Evolve(_state, e switch
            {
                ItemAdded ia => new CartEvent(ia),
                CheckedOut co => new CartEvent(co),
                _ => throw new InvalidOperationException()
            });
        });
        DeferAsync(_state, _ => replyTo.Tell(_state));
    }
}

// ---------- Actor with a timer (for virtual-time tests) ----------
public sealed record StartReservation(string Sku);
public sealed record ReservationExpired(string Sku);

public sealed class ReservationActor : ReceiveActor, IWithTimers
{
    public ITimerScheduler Timers { get; set; } = null!;
    public ReservationActor(IActorRef notify)
    {
        Receive<StartReservation>(m => Timers.StartSingleTimer(m.Sku, new ReservationExpired(m.Sku), TimeSpan.FromMinutes(15)));
        Receive<ReservationExpired>(m => notify.Tell(m));
    }
}
