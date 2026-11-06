using Akka.Serialization;
using Akka.TestKit.Xunit;
using Sales.Domain;
using Sales.Shell;

namespace Sales.Tests;

public class SerializationProbe : TestKit
{
    private object RoundTrip(object o)
    {
        var ser = Sys.Serialization.FindSerializerFor(o);
        var bytes = ser.ToBinary(o);
        var manifest = Akka.Serialization.Serialization.ManifestFor(ser, o);
        var back = Sys.Serialization.Deserialize(bytes, ser.Identifier, manifest);
        File.AppendAllText(Path.Combine(Path.GetTempPath(), "ser.log"), $"{o.GetType().Name} via {ser.GetType().Name}: {System.Text.Encoding.UTF8.GetString(bytes)} -> {back}\n");
        return back;
    }

    [Fact]
    public void Money_with_private_ctor_roundtrips()
    {
        var m = Money.Create(12.5m, "usd").Value as Money;
        var evt = new OrderSubmitted(OrderId.New(), m!, DateTimeOffset.UnixEpoch);
        var back = RoundTrip(evt);
        Assert.Equal(evt, back);
    }

    [Fact]
    public void Envelope_with_union_field_roundtrips()
    {
        var env = new OrderEnvelope("x", new SubmitOrder(OrderId.New()));
        var back = (OrderEnvelope)RoundTrip(env);
        Assert.Equal(env.Command.Value, back.Command.Value);
    }
}
