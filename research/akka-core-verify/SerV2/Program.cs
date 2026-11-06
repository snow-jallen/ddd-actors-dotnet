using Akka.Actor;
using Akka.Hosting;
using Akka.Serialization;
using Akka.Serialization.V2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddAkka("ser", (akka, _) =>
{
    akka.WithCustomSerializer(OrderSerializer.CreateSerializerDetails);
});
using var host = builder.Build();
await host.StartAsync();
var system = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();

object[] samples =
[
    new OrderPlaced("o-1", "c-1"),
    new PaymentRecorded("o-1", new Card("4242", "12/29")),
    new PaymentRecorded("o-1", new Invoice("PO-7")),
];
foreach (var msg in samples)
{
    var ser = system.Serialization.FindSerializerFor(msg);
    var bytes = ser.ToBinary(msg);
    var manifest = Serialization.ManifestFor(ser, msg);
    var back = system.Serialization.Deserialize(bytes, ser.Identifier, manifest);
    Console.WriteLine($"{ser.GetType().Name}#{ser.Identifier} manifest='{manifest}' {bytes.Length}B roundtrip-equal={Equals(back, msg)} -> {back}");
}
await host.StopAsync();

public interface IOrderEvent;

[AkkaSerializable(Manifest = "order-placed-v1")]
public sealed record OrderPlaced(
    [property: AkkaField(0)] string OrderId,
    [property: AkkaField(1)] string CustomerId) : IOrderEvent;

[AkkaSerializable(Manifest = "card-v1")]
public sealed record Card([property: AkkaField(0)] string Last4, [property: AkkaField(1)] string Expiry);
[AkkaSerializable(Manifest = "invoice-v1")]
public sealed record Invoice([property: AkkaField(0)] string PoNumber);

public union PaymentMethod(Card, Invoice);

[AkkaSerializable(Manifest = "payment-recorded-v1")]
public sealed record PaymentRecorded(
    [property: AkkaField(0)] string OrderId,
    [property: AkkaField(1)] PaymentMethod Method) : IOrderEvent;

[AkkaSerializer<IOrderEvent>("order-events", 120001)]
public sealed partial class OrderSerializer : AkkaSerializer
{
    public static partial global::Akka.Serialization.SerializerDetails CreateSerializerDetails(global::Akka.Actor.ExtendedActorSystem system);
}
