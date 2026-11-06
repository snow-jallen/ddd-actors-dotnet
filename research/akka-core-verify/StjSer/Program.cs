using System.Collections.Frozen;
using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;
using Akka.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// 1) Does System.Text.Json (.NET 11) handle C# unions on its own?
Pet pet = new Cat("Tom");
try
{
    var json = JsonSerializer.Serialize(new Adopted("ann", pet));
    Console.WriteLine("STJ union json: " + json);
    var back = JsonSerializer.Deserialize<Adopted>(json);
    Console.WriteLine($"STJ union back: {back} inner={back!.Pet.Value}");
}
catch (Exception ex) { Console.WriteLine("STJ union FAIL: " + ex.GetType().Name + ": " + ex.Message); }

// 2) A manifest-based STJ serializer registered through Akka.Hosting
var b = Host.CreateApplicationBuilder();
b.Services.AddAkka("stj", (akka, _) =>
    akka.WithCustomSerializer("order-json", [typeof(IOrderEvent)], sys => new OrderEventSerializer(sys)));
using var host = b.Build();
await host.StartAsync();
var system = host.Services.GetRequiredService<ActorSystem>();
object evt = new OrderPlaced("o-1", "c-1");
var ser = system.Serialization.FindSerializerFor(evt);
var bytes = ser.ToBinary(evt);
var manifest = Serialization.ManifestFor(ser, evt);
Console.WriteLine($"{ser.GetType().Name} id={ser.Identifier} manifest={manifest} json={System.Text.Encoding.UTF8.GetString(bytes)}");
Console.WriteLine("roundtrip: " + system.Serialization.Deserialize(bytes, ser.Identifier, manifest));
await host.StopAsync();

public record Cat(string Name);
public record Dog(string Name);
public union Pet(Cat, Dog);
public record Adopted(string Owner, Pet Pet);

public interface IOrderEvent;
public sealed record OrderPlaced(string OrderId, string CustomerId) : IOrderEvent;
public sealed record OrderSubmitted(string OrderId) : IOrderEvent;

public sealed class OrderEventSerializer(ExtendedActorSystem system) : SerializerWithStringManifest(system)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    // Stable, explicit manifests: decoupled from CLR type/assembly names.
    private static readonly FrozenDictionary<Type, string> ToManifest = new Dictionary<Type, string>
    {
        [typeof(OrderPlaced)] = "order-placed.v1",
        [typeof(OrderSubmitted)] = "order-submitted.v1",
    }.ToFrozenDictionary();
    private static readonly FrozenDictionary<string, Type> FromManifest =
        ToManifest.ToFrozenDictionary(kv => kv.Value, kv => kv.Key);

    public override int Identifier => 500;
    public override string Manifest(object o) => ToManifest.TryGetValue(o.GetType(), out var m)
        ? m : throw new ArgumentException($"No manifest for {o.GetType()}");
    public override byte[] ToBinary(object obj) => JsonSerializer.SerializeToUtf8Bytes(obj, obj.GetType(), Options);
    public override object FromBinary(byte[] bytes, string manifest) => FromManifest.TryGetValue(manifest, out var t)
        ? JsonSerializer.Deserialize(bytes, t, Options)!
        : throw new System.Runtime.Serialization.SerializationException($"Unknown manifest {manifest}");
}
