using Akka.Actor;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using Shop.Actors;
using Shop.Domain;
using Xunit;

namespace Shop.Tests;

// Pure domain test: no actors at all.
public class OrderStateSpecs
{
    [Fact]
    public void Submitting_an_empty_order_is_rejected()
    {
        var state = OrderState.Empty("o-1").Apply(new OrderPlaced("o-1", "c-1"));
        var decision = state.Decide(new SubmitOrder("o-1"));
        Assert.True(decision is Rejected);
    }
}

// Akka.Hosting.TestKit (xunit v3): spins up a real host with an in-memory journal.
public class OrderActorSpecs(ITestOutputHelper output) : Akka.Hosting.TestKit.TestKit(nameof(OrderActorSpecs), output)
{
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithInMemoryJournal().WithInMemorySnapshotStore();
    }

    [Fact]
    public async Task Order_survives_restart()
    {
        var order = Sys.ActorOf(Props.Create(() => new OrderActor("o-9")));
        order.Tell(new PlaceOrder("o-9", "c-1"), TestActor);
        await ExpectMsgAsync<CommandAccepted>();
        order.Tell(new AddItem("o-9", "sku", 2, 3m), TestActor);
        await ExpectMsgAsync<CommandAccepted>();

        Watch(order);
        Sys.Stop(order);
        await ExpectTerminatedAsync(order);

        var revived = Sys.ActorOf(Props.Create(() => new OrderActor("o-9")));
        revived.Tell(new GetOrder("o-9"), TestActor);
        var view = await ExpectMsgAsync<OrderView>();
        Assert.Equal(6m, view.Total);
    }
}
