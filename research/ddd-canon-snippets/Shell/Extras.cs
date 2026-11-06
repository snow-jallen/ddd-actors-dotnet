using System.Collections.Immutable;
using Akka.Actor;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.Journal;
using Sales.Domain;

namespace Sales.Shell;

// Tag domain events so projections (read models) can subscribe via EventsByTag.
public sealed class OrderTaggingAdapter : IWriteEventAdapter
{
    public string Manifest(object evt) => string.Empty;
    public object ToJournal(object evt) => evt switch
    {
        OrderSubmitted or OrderCancelled => new Tagged(evt, ImmutableHashSet.Create("order", "order-lifecycle")),
        OrderPlaced or LineAdded => new Tagged(evt, ImmutableHashSet.Create("order")),
        _ => evt,
    };
}

public static class PersistenceSetup
{
    public static AkkaConfigurationBuilder WithOrderJournal(this AkkaConfigurationBuilder b) =>
        b.WithInMemoryJournal(adapters => adapters
            .AddWriteEventAdapter<OrderTaggingAdapter>("order-tagger",
                [typeof(OrderPlaced), typeof(LineAdded), typeof(OrderSubmitted), typeof(OrderCancelled)]));
}

// ---------- Anticorruption layer as a translating actor ----------
// Upstream (legacy ERP, conformist-hostile model)
public sealed record ErpOrderShipped(string ORDER_NO, string SHIP_DT, int STATUS_CD);
// Downstream (our ubiquitous language) - an integration event we consume
public sealed record ShipmentConfirmed(OrderId Order, DateOnly ShippedOn);

public sealed class ErpAclActor : ReceiveActor
{
    public ErpAclActor(IActorRef downstream)
    {
        Receive<ErpOrderShipped>(m =>
        {
            if (Translate(m) is { } translated) downstream.Tell(translated);
            else Context.System.EventStream.Publish(new Akka.Event.UnhandledMessage(m, Sender, Self));
        });
    }

    // Pure translation function: unit-testable without actors
    public static ShipmentConfirmed? Translate(ErpOrderShipped m) =>
        m.STATUS_CD == 40 && Guid.TryParseExact(m.ORDER_NO, "N", out var g)
                          && DateOnly.TryParseExact(m.SHIP_DT, "yyyyMMdd", out var d)
            ? new ShipmentConfirmed(new OrderId(g), d)
            : null;
}
