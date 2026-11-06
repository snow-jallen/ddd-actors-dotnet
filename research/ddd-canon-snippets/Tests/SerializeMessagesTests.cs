using Akka.TestKit.Xunit;
using Sales.Domain;
using Sales.Shell;
namespace Sales.Tests;

public class SerializeMessagesOn() : TestKit("akka.actor.serialize-messages = on")
{
    [Fact]
    public void Serialize_messages_exposes_the_union_bug_locally()
    {
        TestActor.Tell(new OrderEnvelope("x", new SubmitOrder(OrderId.New())), Akka.Actor.ActorRefs.NoSender);
        var got = ExpectMsg<OrderEnvelope>();
        Assert.Null(got.Command.Value); // silently lost by Newtonsoft default serializer
    }
}
