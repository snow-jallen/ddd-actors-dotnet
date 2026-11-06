using System.Runtime.Serialization;
using System.Text;
using Akka.Actor;
using Akka.Persistence.Journal;
using Akka.Serialization;

namespace Ops.Verify;

// ---- Versioned event contracts (never delete a published event type) ----
public sealed record OrderPlacedV1(string OrderId, decimal Total);                          // legacy shape
public sealed record OrderPlaced(string OrderId, decimal Total, string Currency);           // current shape (v2)
public sealed record OrderCancelled(string OrderId, string? Reason);                        // Reason added later as nullable

// C# 15 union of the *current* domain events: exhaustive switch in Apply().
public union OrderEvent(OrderPlaced, OrderCancelled);

public static class OrderState
{
    public static string Describe(OrderEvent e) => e switch
    {
        OrderPlaced p    => $"placed {p.OrderId} {p.Total} {p.Currency}",
        OrderCancelled c => $"cancelled {c.OrderId} ({c.Reason ?? "no reason"})",
    };
}

// ---- Read-side upcaster: V1 objects coming out of the journal become V2 ----
public sealed class OrderPlacedUpcaster : IReadEventAdapter
{
    public IEventSequence FromJournal(object evt, string manifest) => evt switch
    {
        OrderPlacedV1 v1 => EventSequence.Single(new OrderPlaced(v1.OrderId, v1.Total, "USD")),
        _ => EventSequence.Single(evt),
    };
}

// ---- Explicit, manifest-versioned serializer (tolerant reader on the bytes) ----
public sealed class OrderEventSerializer(ExtendedActorSystem system) : SerializerWithStringManifest(system)
{
    public override int Identifier => 9001;                 // stable forever; outside Akka's reserved 1..100

    public override string Manifest(object o) => o switch
    {
        OrderPlaced => "order-placed.v2",
        OrderCancelled => "order-cancelled.v1",
        _ => throw new ArgumentException($"Unknown type {o.GetType()}"),
    };

    public override byte[] ToBinary(object obj) => obj switch
    {
        OrderPlaced p => Encoding.UTF8.GetBytes($"{p.OrderId}|{p.Total}|{p.Currency}"),
        OrderCancelled c => Encoding.UTF8.GetBytes($"{c.OrderId}|{c.Reason}"),
        _ => throw new ArgumentException($"Unknown type {obj.GetType()}"),
    };

    public override object FromBinary(byte[] bytes, string manifest)
    {
        var parts = Encoding.UTF8.GetString(bytes).Split('|');
        return manifest switch
        {
            // upcast inside the serializer: old rows still carry the v1 manifest forever
            "order-placed.v1" => new OrderPlaced(parts[0], decimal.Parse(parts[1]), "USD"),
            "order-placed.v2" => new OrderPlaced(parts[0], decimal.Parse(parts[1]), parts[2]),
            "order-cancelled.v1" => new OrderCancelled(parts[0], parts.Length > 1 && parts[1].Length > 0 ? parts[1] : null),
            // SerializationException => remoting drops the message instead of killing the association
            _ => throw new SerializationException($"Unknown manifest [{manifest}] for {nameof(OrderEventSerializer)}"),
        };
    }
}
