using System.Collections.Immutable;

namespace Shop.Domain;

// ---- Commands (messages that cross the shard boundary: plain sealed records) ----
public interface IOrderCommand { string OrderId { get; } }
public sealed record PlaceOrder(string OrderId, string CustomerId) : IOrderCommand;
public sealed record AddItem(string OrderId, string Sku, int Quantity, decimal UnitPrice) : IOrderCommand;
public sealed record SubmitOrder(string OrderId) : IOrderCommand;
public sealed record GetOrder(string OrderId) : IOrderCommand;

// ---- Events (persisted: plain sealed records + marker interface) ----
public interface IOrderEvent { string OrderId { get; } }
public sealed record OrderPlaced(string OrderId, string CustomerId) : IOrderEvent;
public sealed record ItemAdded(string OrderId, string Sku, int Quantity, decimal UnitPrice) : IOrderEvent;
public sealed record OrderSubmitted(string OrderId) : IOrderEvent;

// ---- C# 15 unions: closed sets used INSIDE the domain model ----
public union OrderEvent(OrderPlaced, ItemAdded, OrderSubmitted);

public sealed record Accepted(ImmutableArray<OrderEvent> Events);
public sealed record Rejected(string Reason);
public union Decision(Accepted, Rejected);

// ---- Replies ----
public sealed record OrderView(string OrderId, string Status, decimal Total, int Lines);
public sealed record CommandRejected(string OrderId, string Reason);
public sealed record CommandAccepted(string OrderId, long Version);

public enum OrderStatus { Empty, Open, Submitted }

public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice);

public sealed record OrderState(string OrderId, OrderStatus Status, string? CustomerId, ImmutableList<OrderLine> Lines)
{
    public static OrderState Empty(string id) => new(id, OrderStatus.Empty, null, []);
    public decimal Total => Lines.Sum(l => l.Quantity * l.UnitPrice);

    // Exhaustive switch over a union: adding a case to OrderEvent is a compile error here.
    public OrderState Apply(OrderEvent e) => e switch
    {
        OrderPlaced p    => this with { Status = OrderStatus.Open, CustomerId = p.CustomerId },
        ItemAdded i      => this with { Lines = Lines.Add(new OrderLine(i.Sku, i.Quantity, i.UnitPrice)) },
        OrderSubmitted   => this with { Status = OrderStatus.Submitted },
        null             => this,  // default(OrderEvent) has a null Value
    };

    // Pure decision function: command -> Decision union
    public Decision Decide(IOrderCommand cmd) => (cmd, Status) switch
    {
        (PlaceOrder p, OrderStatus.Empty) => Accept(new OrderPlaced(p.OrderId, p.CustomerId)),
        (PlaceOrder, _)                   => new Rejected("order already exists"),
        (AddItem a, OrderStatus.Open) when a.Quantity > 0
                                          => Accept(new ItemAdded(a.OrderId, a.Sku, a.Quantity, a.UnitPrice)),
        (AddItem, OrderStatus.Open)       => new Rejected("quantity must be positive"),
        (AddItem, _)                      => new Rejected($"cannot add items when {Status}"),
        (SubmitOrder, OrderStatus.Open) when !Lines.IsEmpty
                                          => Accept(new OrderSubmitted(OrderId)),
        (SubmitOrder, _)                  => new Rejected("order cannot be submitted"),
        _                                 => new Rejected($"unsupported command {cmd.GetType().Name}"),
    };

    private static Decision Accept(params OrderEvent[] events) => new Accepted([.. events]);

    // Bridge from the persisted (boxed) world back into the union.
    public static OrderEvent ToUnion(IOrderEvent e) => e switch
    {
        OrderPlaced p => p,
        ItemAdded i => i,
        OrderSubmitted s => s,
        _ => throw new ArgumentOutOfRangeException(nameof(e), e.GetType().Name)
    };
}
